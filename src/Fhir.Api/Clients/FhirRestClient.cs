using System.Diagnostics;
using System.Net;
using Fhir.Api.Configuration;
using Fhir.Api.Serialization;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;
using RestSharp;

namespace Fhir.Api.Clients;

/// <summary>
/// Minimal client for the FHIR RESTful API (https://hl7.org/fhir/R4/http.html).
/// It never throws on 4xx/5xx: tests assert on status codes and OperationOutcomes.
/// Transient failures are retried so a slow shared server doesn't fail the suite.
/// </summary>
public sealed class FhirRestClient : IDisposable
{
    private const string FhirJsonContentType = "application/fhir+json";
    private static readonly HttpStatusCode[] TransientStatusCodes =
        [HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable, HttpStatusCode.GatewayTimeout];

    private readonly RestClient _client;
    private readonly int _maxRetries;

    public FhirRestClient(FhirSettings settings)
    {
        _maxRetries = settings.MaxRetries;
        _client = new RestClient(new RestClientOptions(settings.BaseUrl.TrimEnd('/') + "/")
        {
            Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds),
            ThrowOnAnyError = false
        });
        _client.AddDefaultHeader("Accept", FhirJsonContentType);
    }

    public Task<FhirResponse> CapabilitiesAsync() => SendAsync(new RestRequest("metadata"));

    public Task<FhirResponse> CreateAsync(Resource resource) =>
        SendAsync(WithBody(new RestRequest(resource.TypeName, Method.Post), resource));

    /// <summary>Sends any JSON as-is; used for payloads the SDK would refuse to build.</summary>
    public Task<FhirResponse> CreateRawAsync(string resourceType, string json) =>
        SendAsync(new RestRequest(resourceType, Method.Post).AddStringBody(json, FhirJsonContentType));

    public Task<FhirResponse> ReadAsync(string resourceType, string id) =>
        SendAsync(new RestRequest($"{resourceType}/{id}"));

    public Task<FhirResponse> VReadAsync(string resourceType, string id, string versionId) =>
        SendAsync(new RestRequest($"{resourceType}/{id}/_history/{versionId}"));

    public Task<FhirResponse> UpdateAsync(Resource resource, string? urlId = null) =>
        SendAsync(WithBody(new RestRequest($"{resource.TypeName}/{urlId ?? resource.Id}", Method.Put), resource));

    public Task<FhirResponse> DeleteAsync(string resourceType, string id) =>
        SendAsync(new RestRequest($"{resourceType}/{id}", Method.Delete));

    public Task<FhirResponse> SearchAsync(string resourceType, IReadOnlyDictionary<string, string> parameters)
    {
        // Servers may reuse results of an identical recent search (HAPI does), which hides
        // resources created moments ago. Tests need fresh results, so the cache is bypassed.
        var request = new RestRequest(resourceType).AddHeader("Cache-Control", "no-cache");
        foreach (var (name, value) in parameters) request.AddQueryParameter(name, value);
        return SendAsync(request);
    }

    private static RestRequest WithBody(RestRequest request, Resource resource) =>
        request.AddStringBody(FhirJson.Serialize(resource), FhirJsonContentType);

    private async Task<FhirResponse> SendAsync(RestRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        RestResponse response;
        var attempt = 0;

        while (true)
        {
            attempt++;
            response = await _client.ExecuteAsync(request);
            var transient = response.StatusCode == 0 || TransientStatusCodes.Contains(response.StatusCode);
            if (!transient || attempt > _maxRetries) break;
            await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
        }

        return new FhirResponse(
            request.Method.ToString().ToUpperInvariant(),
            response.ResponseUri?.ToString() ?? request.Resource,
            response.StatusCode,
            Header(response, "Location") ?? Header(response, "Content-Location"),
            Header(response, "ETag"),
            response.Content ?? string.Empty,
            stopwatch.Elapsed,
            attempt);
    }

    private static string? Header(RestResponse response, string name) =>
        response.Headers?.FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase))?.Value?.ToString()
        ?? response.ContentHeaders?.FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase))?.Value?.ToString();

    public void Dispose() => _client.Dispose();
}
