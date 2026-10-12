using Fhir.ApiTests.Support;
using Hl7.Fhir.Model;

namespace Fhir.ApiTests.StepDefinitions;

[Binding]
public sealed class CommonSteps(ScenarioData data, QualitySettings quality)
{
    [Then("the response status code should be {int}")]
    public void ThenTheResponseStatusCodeShouldBe(int expected) =>
        Assert.That((int)data.Response.StatusCode, Is.EqualTo(expected), $"Unexpected status code.\n{data.Response}");

    [Then("the response time should be within the agreed limit")]
    public void ThenTheResponseTimeShouldBeWithinTheAgreedLimit() =>
        Assert.That(data.Response.Elapsed.TotalMilliseconds, Is.LessThan(quality.MaxResponseTimeMs),
            $"{data.Response.Method} {data.Response.Url} took {data.Response.Elapsed.TotalMilliseconds:F0} ms");

    [Then("the response should be an OperationOutcome with an error mentioning {string}")]
    public void ThenTheResponseShouldBeAnOperationOutcomeWithAnErrorMentioning(string text)
    {
        var outcome = data.Response.Resource<OperationOutcome>();
        var errors = outcome.Issue
            .Where(issue => issue.Severity is OperationOutcome.IssueSeverity.Error or OperationOutcome.IssueSeverity.Fatal)
            .Select(issue => issue.Diagnostics ?? issue.Details?.Text ?? string.Empty)
            .ToList();

        Assert.That(errors, Is.Not.Empty, "The OperationOutcome has no error issues");
        Assert.That(errors, Has.Some.Contains(text).IgnoreCase, $"No error mentions '{text}'");
    }

    [Then("the search should return a {string} bundle with {int} match(es)")]
    public void ThenTheSearchShouldReturnABundleWithMatches(string bundleType, int matches)
    {
        var bundle = data.Response.Resource<Bundle>();
        var matchEntries = bundle.Entry.Count(e => e.Search?.Mode == Bundle.SearchEntryMode.Match);

        Assert.Multiple(() =>
        {
            Assert.That(bundle.Type?.ToString(), Is.EqualTo(bundleType).IgnoreCase);
            Assert.That(bundle.Total, Is.EqualTo(matches), "Bundle.total");
            Assert.That(matchEntries, Is.EqualTo(matches), "Entries with search.mode = match");
        });
    }
}
