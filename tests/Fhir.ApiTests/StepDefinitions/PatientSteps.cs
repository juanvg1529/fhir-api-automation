using Fhir.Api.Clients;
using Fhir.ApiTests.Support;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Fhir.ApiTests.StepDefinitions;

[Binding]
public sealed class PatientSteps(FhirRestClient fhir, ScenarioData data)
{
    // ---------- Given ----------

    [Given("a new patient:")]
    public void GivenANewPatient(DataTable table)
    {
        var row = table.Rows.Single();
        data.Patient = PatientBuilder.Create(data.IdentifierValue, row["family"], row["given"], row["gender"], row["birthDate"]);
    }

    [Given("a registered patient")]
    public async Task GivenARegisteredPatient()
    {
        data.Patient = PatientBuilder.Default(data.IdentifierValue);
        await WhenIRegisterThePatient();
        Assert.That((int)data.Response.StatusCode, Is.EqualTo(201), $"Precondition failed: patient not created.\n{data.Response}");
    }

    // ---------- When ----------

    [When("I register the patient")]
    public async Task WhenIRegisterThePatient()
    {
        var response = await data.SendAsync(() => fhir.CreateAsync(data.Patient!));
        if ((int)response.StatusCode != 201) return;

        var created = response.Resource<Patient>();
        data.TrackCreated(created);
        data.Patient!.Id = created.Id;
        data.OriginalPatient = (Patient)data.Patient.DeepCopy();
    }

    [When("I change the patient's given name to {string}")]
    public async Task WhenIChangeThePatientsGivenName(string given)
    {
        data.Patient!.Name[0].Given = [given];
        await data.SendAsync(() => fhir.UpdateAsync(data.Patient));
    }

    [When("I delete the patient")]
    public async Task WhenIDeleteThePatient()
    {
        await data.SendAsync(() => fhir.DeleteAsync("Patient", data.PatientId));
    }

    [When("I update the patient sending a different id in the body")]
    public async Task WhenIUpdateThePatientSendingADifferentIdInTheBody()
    {
        var mismatched = (Patient)data.Patient!.DeepCopy();
        mismatched.Id = "different-id-123";
        await data.SendAsync(() => fhir.UpdateAsync(mismatched, urlId: data.PatientId));
    }

    [When("I read a patient that does not exist")]
    public async Task WhenIReadAPatientThatDoesNotExist() =>
        await data.SendAsync(() => fhir.ReadAsync("Patient", $"never-existed-{Guid.NewGuid():N}"[..40]));

    [When("I submit this {string}:")]
    public async Task WhenISubmitThis(string resourceType, string json) =>
        await data.SendAsync(() => fhir.CreateRawAsync(resourceType, json));

    [When("I search patients by the patient's identifier")]
    public async Task WhenISearchPatientsByThePatientsIdentifier() =>
        await data.SendAsync(() => fhir.SearchAsync("Patient", new Dictionary<string, string>
        {
            ["identifier"] = $"{FhirSystems.PortfolioIdentifier}|{data.IdentifierValue}"
        }));

    [When("I search patients by the patient's family name and birth date")]
    public async Task WhenISearchPatientsByFamilyNameAndBirthDate() =>
        await data.SendAsync(() => fhir.SearchAsync("Patient", new Dictionary<string, string>
        {
            ["family"] = data.Patient!.Name[0].Family!,
            ["birthdate"] = data.Patient.BirthDate!,
            ["_sort"] = "-_lastUpdated",
            ["_count"] = "50"
        }));

    [When("I search patients by an identifier that does not exist")]
    public async Task WhenISearchPatientsByAnIdentifierThatDoesNotExist() =>
        await data.SendAsync(() => fhir.SearchAsync("Patient", new Dictionary<string, string>
        {
            ["identifier"] = $"{FhirSystems.PortfolioIdentifier}|missing-{Guid.NewGuid():N}"
        }));

    // ---------- Then ----------

    [Then("the server should assign an id and version {string}")]
    public void ThenTheServerShouldAssignAnIdAndVersion(string version)
    {
        var created = data.Response.Resource<Patient>();
        Assert.Multiple(() =>
        {
            Assert.That(created.Id, Is.Not.Null.And.Not.Empty, "id");
            Assert.That(created.Meta?.VersionId, Is.EqualTo(version), "meta.versionId");
            Assert.That(created.Meta?.LastUpdated, Is.Not.Null, "meta.lastUpdated");
            Assert.That(data.Response.ETag, Is.EqualTo($"W/\"{version}\""), "ETag header");
        });
    }

    [Then("the server should report version {string}")]
    public void ThenTheServerShouldReportVersion(string version) =>
        Assert.That(data.Response.Resource<Patient>().Meta?.VersionId, Is.EqualTo(version));

    [Then("the Location header should point to version {string} of the patient")]
    public void ThenTheLocationHeaderShouldPointToVersionOfThePatient(string version) =>
        Assert.That(data.Response.Location, Does.EndWith($"/Patient/{data.PatientId}/_history/{version}"));

    [Then("reading the patient should return the same demographics")]
    public async Task ThenReadingThePatientShouldReturnTheSameDemographics()
    {
        var read = await fhir.ReadAsync("Patient", data.PatientId);
        Assert.That((int)read.StatusCode, Is.EqualTo(200), read.ToString());
        AssertSameDemographics(read.Resource<Patient>(), data.Patient!);
    }

    [Then("version {string} of the patient should keep the original given name")]
    public async Task ThenVersionOfThePatientShouldKeepTheOriginalGivenName(string version)
    {
        var old = await fhir.VReadAsync("Patient", data.PatientId, version);
        Assert.That((int)old.StatusCode, Is.EqualTo(200), old.ToString());
        Assert.That(old.Resource<Patient>().Name[0].Given, Is.EqualTo(data.OriginalPatient!.Name[0].Given));
    }

    [Then("reading the patient should return status {int}")]
    public async Task ThenReadingThePatientShouldReturnStatus(int status)
    {
        var read = await fhir.ReadAsync("Patient", data.PatientId);
        Assert.That((int)read.StatusCode, Is.EqualTo(status), read.ToString());

        // Already deleted: nothing left to clean up.
        if (status == 410) data.CreatedResources.TryPop(out _);
    }

    [Then("the bundle should contain the patient")]
    public void ThenTheBundleShouldContainThePatient()
    {
        var ids = data.Response.Resource<Bundle>().Entry.Select(e => e.Resource?.Id);
        Assert.That(ids, Does.Contain(data.PatientId));
    }

    private static void AssertSameDemographics(Patient actual, Patient expected) => Assert.Multiple(() =>
    {
        Assert.That(actual.Name[0].Family, Is.EqualTo(expected.Name[0].Family), "family");
        Assert.That(actual.Name[0].Given, Is.EqualTo(expected.Name[0].Given), "given");
        Assert.That(actual.Gender, Is.EqualTo(expected.Gender), "gender");
        Assert.That(actual.BirthDate, Is.EqualTo(expected.BirthDate), "birthDate");
        Assert.That(actual.Identifier.Select(i => (i.System, i.Value)),
            Is.EquivalentTo(expected.Identifier.Select(i => (i.System, i.Value))), "identifier");
    });
}
