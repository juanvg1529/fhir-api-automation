namespace Fhir.ApiTests.Support;

/// <summary>Non-functional limits read from the "Quality" section of the configuration.</summary>
public sealed class QualitySettings
{
    public int MaxResponseTimeMs { get; init; } = 8000;
}
