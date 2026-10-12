using Microsoft.Extensions.Configuration;

namespace Fhir.Api.Configuration;

/// <summary>
/// Settings from appsettings.json, an optional appsettings.{TEST_ENV}.json and
/// environment variables prefixed with FHIR_ (e.g. FHIR_Fhir__BaseUrl).
/// </summary>
public sealed class FhirSettings
{
    public string BaseUrl { get; init; } = "https://hapi.fhir.org/baseR4";
    public int TimeoutSeconds { get; init; } = 60;

    /// <summary>Retries for transient failures (timeouts, 502, 503, 504).</summary>
    public int MaxRetries { get; init; } = 2;

    public static IConfiguration BuildConfiguration(string basePath)
    {
        var environment = Environment.GetEnvironmentVariable("TEST_ENV") ?? "default";

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables(prefix: "FHIR_")
            .Build();
    }

    public static FhirSettings Load(IConfiguration configuration) =>
        configuration.GetSection("Fhir").Get<FhirSettings>()
        ?? throw new InvalidOperationException("Missing 'Fhir' section in configuration.");
}
