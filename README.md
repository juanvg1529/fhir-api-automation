# FHIR R4 API Automation

[![FHIR API Tests](https://github.com/juanvg1529/fhir-api-automation/actions/workflows/fhir-tests.yml/badge.svg)](https://github.com/juanvg1529/fhir-api-automation/actions/workflows/fhir-tests.yml)
[![Allure Report](https://img.shields.io/badge/report-Allure-orange)](https://juanvg1529.github.io/fhir-api-automation/)
![HL7 FHIR R4](https://img.shields.io/badge/HL7%20FHIR-R4%204.0.1-e44d26)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)

BDD test automation for a healthcare interoperability API: the **HL7 FHIR R4** RESTful interface, tested against the public [HAPI FHIR](https://hapi.fhir.org/) reference server with C# / .NET 10, Reqnroll and the official **Firely .NET SDK**.

It covers what matters when clinical systems exchange data: patient identity and versioning, clinical coding (LOINC, UCUM), referential integrity, and standards-compliant error handling.

**→ [Latest Allure report](https://juanvg1529.github.io/fhir-api-automation/)** · **[Test plan & traceability](docs/test-plan.md)**

## Why FHIR

FHIR (Fast Healthcare Interoperability Resources) is the HL7 standard that hospitals, labs, EHRs and health apps use to exchange clinical data over REST. Testing it goes beyond status codes: a passing test must also prove the data is **clinically correct**, meaning the right patient, the right code system and the right unit. That's the focus of this suite.

## Coverage

19 scenarios across 5 features:

| Feature | What is verified |
|---|---|
| Server capabilities | `GET /metadata` declares FHIR 4.0.1 and every interaction the suite depends on |
| Patient lifecycle | Create → `201` with `Location`, `ETag` and `meta.versionId`; update creates version 2 while **version 1 stays readable** (`vread`); delete → read returns **`410 Gone`**, not `404`; a patient referenced by an Observation **cannot be deleted** (`409`) |
| Patient search | By business identifier (exact, `Bundle.total = 1`), by family name + birth date, unknown identifier → empty `searchset` bundle |
| Validation | Invalid code (`gender: robot`), non-FHIR date format, wrong resource type, body/URL id mismatch, unknown id → `4xx` with an **OperationOutcome** that names the problem |
| Vital signs | Heart rate, body temperature, respiratory rate, body weight and SpO₂ stored as `final` vital-sign Observations with the right **LOINC** code, **UCUM** unit and patient reference; search by subject + LOINC code |

Tags: `@smoke` (capabilities, patient registration, vital signs), `@regression`, `@negative`.

## Stack

| Concern | Tool |
|---|---|
| Language / runtime | C# 14, .NET 10 |
| FHIR model & JSON | [Firely .NET SDK](https://github.com/FirelyTeam/firely-net-sdk) (`Hl7.Fhir.R4`) |
| BDD / runner | Reqnroll + NUnit |
| HTTP | RestSharp |
| Reporting | Allure (GitHub Pages) |
| CI/CD | GitHub Actions |

## Architecture

```
src/Fhir.Api/                       ← reusable FHIR client (no test code)
├── Clients/FhirRestClient.cs       create, read, vread, update, delete, search, capabilities
├── Clients/FhirResponse.cs         status + Location/ETag + body + timing + attempts
├── Serialization/FhirJson.cs       FHIR JSON via the Firely SDK converters
└── Configuration/FhirSettings.cs   appsettings → appsettings.{TEST_ENV} → FHIR_* env vars

tests/Fhir.ApiTests/
├── Features/                       Gherkin scenarios
├── StepDefinitions/                Capability, Patient, Observation and common steps
├── Hooks/                          shared client, DI, Allure attachments, cleanup
└── Support/                        PatientBuilder, ObservationBuilder, code systems, ScenarioData
```

## Design decisions

- **Typed FHIR models from the official SDK, raw HTTP under control.** Resources are built and parsed with Firely's `Patient`, `Observation`, `Bundle` and `OperationOutcome` classes, so FHIR rules (choice types like `value[x]`, `resourceType`) are handled correctly. Requests go through a thin RestSharp client that never throws, so negative tests can assert on any status code.
- **Invalid payloads are sent as raw JSON.** The SDK refuses to build an invalid resource (`gender: robot`), which is good for production code but useless for negative testing. Validation scenarios post the exact JSON from the feature file.
- **Errors must explain themselves.** Every negative scenario checks the `OperationOutcome` has an `error` issue that mentions the actual problem, not just a `400`.
- **Isolation through business identifiers.** Each scenario gets a unique identifier in the suite's own namespace (`urn:jdvg:fhir-portfolio`), so features run in parallel on a shared public server without seeing each other's data.
- **Cleanup respects referential integrity.** Created resources are deleted newest-first: Observations before the Patient they reference. A run leaves nothing behind on the public server.
- **Transient failures are retried, real failures are not.** Timeouts and `502`/`503`/`504` from the shared server are retried with backoff (the attempt count is in the report). Any `4xx` or assertion failure is reported immediately.
- **Searches bypass the server cache.** See the finding below.

## Finding: search results served from cache

While running the suite repeatedly, *Find a patient by family name and birth date* passed on the first run and then failed consistently. HAPI reuses the results of an identical recent search, so a patient created seconds earlier was missing from the bundle. Reproduced outside the suite:

```
search family=CacheProbe&birthdate=1990-05-15        → (cache warmed)
create Patient CacheProbe 1990-05-15                  → 201
same search                                           → patient NOT in bundle
same search + header "Cache-Control: no-cache"        → patient in bundle
```

This is documented, configurable HAPI behaviour, not a defect. For tests, the client sends `Cache-Control: no-cache` on every search, and the scenario now passes reliably. The lesson for a real integration: a consumer that creates and then searches immediately can get stale results unless it opts out of caching.

## Running locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet test                                   # all scenarios
dotnet test --filter "Category=smoke"
FHIR_Fhir__BaseUrl=https://server.fire.ly/r4 dotnet test   # another FHIR R4 server
```

Allure report (needs Java):

```bash
npx allure-commandline generate tests/Fhir.ApiTests/bin/Debug/net10.0/allure-results --clean --single-file -o allure-report
open allure-report/index.html
```

## Next steps

- [ ] Profile validation with `$validate` against the US Core / international vital signs profiles
- [ ] FHIR transactions (`Bundle` of type `transaction`) and conditional create
- [ ] HL7 v2 (ADT/ORU) message parsing and v2 → FHIR mapping checks
- [ ] Run against a local HAPI container in CI for full isolation

---

Built by **Juan Diego Vera Godoy**, QA Automation Engineer / SDET with 3 years testing healthcare software and a background in bioengineering · [LinkedIn](https://www.linkedin.com/in/juan-diego-vera-godoy-3949a6183) · See also: [.NET API Automation Framework](https://github.com/juanvg1529/dotnet-api-automation-framework)
