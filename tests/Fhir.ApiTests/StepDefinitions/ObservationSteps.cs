using Fhir.Api.Clients;
using Fhir.ApiTests.Support;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Fhir.ApiTests.StepDefinitions;

[Binding]
public sealed class ObservationSteps(FhirRestClient fhir, ScenarioData data)
{
    [Given("the patient has a heart rate observation of {decimal} {string}")]
    public async Task GivenThePatientHasAHeartRateObservation(decimal value, string unit)
    {
        await WhenIRecordAnObservation("Heart rate", "8867-4", value, unit);
        Assert.That((int)data.Response.StatusCode, Is.EqualTo(201), $"Precondition failed: observation not created.\n{data.Response}");
    }

    [When("I record a {string} observation with LOINC {string} and value {decimal} {string}")]
    public async Task WhenIRecordAnObservation(string display, string loinc, decimal value, string unit)
    {
        var observation = ObservationBuilder.VitalSign(data.PatientId, loinc, display, value, unit);
        var response = await data.SendAsync(() => fhir.CreateAsync(observation));
        if ((int)response.StatusCode != 201) return;

        data.Observation = response.Resource<Observation>();
        data.TrackCreated(data.Observation);
    }

    [When("I search the patient's observations with LOINC {string}")]
    public async Task WhenISearchThePatientsObservationsWithLoinc(string loinc) =>
        await data.SendAsync(() => fhir.SearchAsync("Observation", new Dictionary<string, string>
        {
            ["subject"] = $"Patient/{data.PatientId}",
            ["code"] = $"{FhirSystems.Loinc}|{loinc}"
        }));

    [Then("the stored observation should be a final vital sign for the patient")]
    public async Task ThenTheStoredObservationShouldBeAFinalVitalSignForThePatient()
    {
        var stored = await ReadStoredObservation();
        Assert.Multiple(() =>
        {
            Assert.That(stored.Status, Is.EqualTo(ObservationStatus.Final), "status");
            Assert.That(stored.Subject?.Reference, Is.EqualTo($"Patient/{data.PatientId}"), "subject");
            Assert.That(stored.Category.SelectMany(c => c.Coding).Select(c => (c.System, c.Code)),
                Does.Contain((FhirSystems.ObservationCategory, "vital-signs")), "category");
            Assert.That(stored.Effective, Is.Not.Null, "effective[x]");
        });
    }

    [Then("the stored observation should have LOINC {string} and value {decimal} {string}")]
    public async Task ThenTheStoredObservationShouldHaveLoincAndValue(string loinc, decimal value, string unit)
    {
        var stored = await ReadStoredObservation();
        var coding = stored.Code.Coding.Single();
        var quantity = stored.Value as Quantity;

        Assert.Multiple(() =>
        {
            Assert.That((coding.System, coding.Code), Is.EqualTo((FhirSystems.Loinc, loinc)), "code");
            Assert.That(quantity, Is.Not.Null, "value[x] should be a Quantity");
            Assert.That(quantity?.Value, Is.EqualTo(value), "value");
            Assert.That((quantity?.System, quantity?.Code), Is.EqualTo((FhirSystems.Ucum, unit)), "UCUM unit");
        });
    }

    private async Task<Observation> ReadStoredObservation()
    {
        var read = await fhir.ReadAsync("Observation", data.ObservationId);
        Assert.That((int)read.StatusCode, Is.EqualTo(200), read.ToString());
        return read.Resource<Observation>();
    }
}
