using Fhir.Api.Clients;
using Hl7.Fhir.Model;

namespace Fhir.ApiTests.Support;

/// <summary>Per-scenario state shared between step classes through context injection.</summary>
public sealed class ScenarioData
{
    /// <summary>Business identifier unique to this scenario, so parallel runs never see each other's data.</summary>
    public string IdentifierValue { get; } = $"jdvg-{Guid.NewGuid():N}";

    /// <summary>The patient as the scenario expects the server to hold it.</summary>
    public Patient? Patient { get; set; }

    /// <summary>The original patient, before any update in the scenario.</summary>
    public Patient? OriginalPatient { get; set; }

    public Observation? Observation { get; set; }
    public FhirResponse? LastResponse { get; set; }

    /// <summary>Resources created by the scenario, deleted in reverse order after it.</summary>
    public Stack<(string Type, string Id)> CreatedResources { get; } = new();

    public FhirResponse Response =>
        LastResponse ?? throw new InvalidOperationException("No request has been sent in this scenario yet.");

    public string PatientId =>
        Patient?.Id ?? throw new InvalidOperationException("No patient has been registered in this scenario yet.");

    public string ObservationId =>
        Observation?.Id ?? throw new InvalidOperationException("No observation has been recorded in this scenario yet.");

    /// <summary>Remembers a resource the server created so the AfterScenario hook can delete it.</summary>
    public void TrackCreated(Resource resource) =>
        CreatedResources.Push((resource.TypeName, resource.Id ?? throw new InvalidOperationException($"Server returned a {resource.TypeName} without an id.")));

    public async Task<FhirResponse> SendAsync(Func<Task<FhirResponse>> request)
    {
        LastResponse = await request();
        return LastResponse;
    }
}
