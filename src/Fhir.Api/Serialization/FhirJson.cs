using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Fhir.Api.Serialization;

/// <summary>
/// FHIR JSON (de)serialization with the official Firely SDK. FHIR JSON has
/// rules plain System.Text.Json doesn't know (resourceType discriminator,
/// primitive extensions, choice types like value[x]), so the SDK's converters are used.
/// </summary>
public static class FhirJson
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions().ForFhir();

    public static string Serialize(Resource resource) => JsonSerializer.Serialize(resource, resource.GetType(), Options);

    public static T Deserialize<T>(string json) where T : Resource =>
        JsonSerializer.Deserialize<T>(json, Options)
        ?? throw new InvalidOperationException($"Body could not be read as {typeof(T).Name}.");
}
