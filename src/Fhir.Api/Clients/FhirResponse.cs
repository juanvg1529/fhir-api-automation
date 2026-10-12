using System.Net;
using Fhir.Api.Serialization;
using Hl7.Fhir.Model;

namespace Fhir.Api.Clients;

/// <summary>
/// Everything a test needs from an HTTP exchange with a FHIR server:
/// status, the FHIR-relevant headers, the raw body and timing.
/// </summary>
public sealed record FhirResponse(
    string Method,
    string Url,
    HttpStatusCode StatusCode,
    string? Location,
    string? ETag,
    string Body,
    TimeSpan Elapsed,
    int Attempts)
{
    public T Resource<T>() where T : Resource => FhirJson.Deserialize<T>(Body);

    public override string ToString() =>
        $"{Method} {Url}\nStatus: {(int)StatusCode} {StatusCode} | Time: {Elapsed.TotalMilliseconds:F0} ms | Attempts: {Attempts}\n" +
        $"Location: {Location}\nETag: {ETag}\n\n{Body}";
}
