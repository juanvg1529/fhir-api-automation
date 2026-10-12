using Fhir.Api.Clients;
using Fhir.ApiTests.Support;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Task = System.Threading.Tasks.Task;

namespace Fhir.ApiTests.StepDefinitions;

[Binding]
public sealed class CapabilitySteps(FhirRestClient fhir, ScenarioData data)
{
    [When("I request the server's capability statement")]
    public async Task WhenIRequestTheServersCapabilityStatement() =>
        await data.SendAsync(fhir.CapabilitiesAsync);

    [Then("the server should declare FHIR version {string}")]
    public void ThenTheServerShouldDeclareFhirVersion(string version)
    {
        var capabilities = data.Response.Resource<CapabilityStatement>();
        Assert.That(capabilities.FhirVersion?.GetLiteral(), Is.EqualTo(version));
    }

    [Then("the server should support these interactions:")]
    public void ThenTheServerShouldSupportTheseInteractions(DataTable table)
    {
        var resources = data.Response.Resource<CapabilityStatement>()
            .Rest.Single(r => r.Mode == CapabilityStatement.RestfulCapabilityMode.Server)
            .Resource;

        Assert.Multiple(() =>
        {
            foreach (var row in table.Rows)
            {
                var resourceType = row["resource"];
                var declared = resources.SingleOrDefault(r => r.Type == resourceType);
                Assert.That(declared, Is.Not.Null, $"{resourceType} is not declared");
                if (declared is null) continue;

                var supported = declared.Interaction.Select(i => i.Code?.GetLiteral()).ToList();
                var expected = row["interactions"].Split(',', StringSplitOptions.TrimEntries);
                Assert.That(supported, Is.SupersetOf(expected), $"{resourceType} interactions");
            }
        });
    }
}
