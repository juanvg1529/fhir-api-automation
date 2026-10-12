using System.Text;
using Allure.Net.Commons;
using Fhir.Api.Clients;
using Fhir.Api.Configuration;
using Fhir.ApiTests.Support;
using Microsoft.Extensions.Configuration;
using Reqnroll.BoDi;

namespace Fhir.ApiTests.Hooks;

[Binding]
public sealed class FhirHooks(IObjectContainer container, ScenarioData data)
{
    private static FhirSettings _settings = null!;
    private static QualitySettings _quality = null!;
    private static FhirRestClient _client = null!;

    [BeforeTestRun]
    public static void CreateSharedClient()
    {
        var configuration = FhirSettings.BuildConfiguration(AppContext.BaseDirectory);
        _settings = FhirSettings.Load(configuration);
        _quality = configuration.GetSection("Quality").Get<QualitySettings>() ?? new QualitySettings();
        _client = new FhirRestClient(_settings);
    }

    [AfterTestRun]
    public static void DisposeSharedClient() => _client.Dispose();

    [BeforeScenario(Order = 0)]
    public void RegisterDependencies()
    {
        container.RegisterInstanceAs(_settings);
        container.RegisterInstanceAs(_quality);
        container.RegisterInstanceAs(_client, dispose: false);
    }

    [AfterScenario(Order = 0)]
    public void AttachLastResponse()
    {
        if (data.LastResponse is { } response)
            AllureApi.AddAttachment("Last response", "text/plain", Encoding.UTF8.GetBytes(response.ToString()), ".txt");
    }

    /// <summary>
    /// Deletes what the scenario created, newest first: observations reference
    /// patients, and the server refuses to delete a referenced patient.
    /// </summary>
    [AfterScenario(Order = 100)]
    public async Task DeleteCreatedResources()
    {
        while (data.CreatedResources.TryPop(out var resource))
        {
            await _client.DeleteAsync(resource.Type, resource.Id);
        }
    }
}
