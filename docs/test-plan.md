# Test Plan — FHIR R4 RESTful API

| | |
|---|---|
| **System under test** | HAPI FHIR public R4 server (`https://hapi.fhir.org/baseR4`), FHIR 4.0.1 |
| **Specification** | [HL7 FHIR R4 RESTful API](https://hl7.org/fhir/R4/http.html) |
| **Author** | Juan Diego Vera Godoy |
| **Last updated** | 2026-10-11 |

## 1. Objective

Verify that a FHIR R4 server handles patient identity and vital-sign data the way the specification requires, so clinical systems integrating with it can trust:

- the data they store,
- the versions they read,
- the errors they receive.

## 2. Scope

**In scope**
- `CapabilityStatement`
- `Patient`: create, read, vread, update, delete, search
- `Observation` (vital signs): create, read, delete, search
- Error handling with `OperationOutcome`

**Out of scope**
- Profile conformance (`$validate`): next step.
- Transactions and batch.
- `PATCH`.
- XML format.
- SMART-on-FHIR authorisation: the public server is open.
- Performance and load.

## 3. Quality characteristics (ISO/IEC 25010)

| Characteristic | Focus | Example check |
|---|---|---|
| Functional correctness | Data stored and returned exactly | Demographics read back equal what was sent |
| Interoperability | Standard codes and units | LOINC code system and UCUM units preserved |
| Integrity | Versioning and references | Old versions stay readable; referenced patients can't be deleted |
| Reliability | Graceful rejection of bad input | `4xx` + `OperationOutcome`, never `5xx` |
| Time behaviour | Acceptable latency | Main operations within 8,000 ms (public server, configurable) |

## 4. Approach

- **Black-box API testing** of the RESTful interface, written as BDD scenarios (Reqnroll).
- **Typed resources** built with the Firely SDK. Invalid payloads are sent as raw JSON.
- **Isolation:** unique business identifier per scenario; resources deleted newest-first after each scenario.
- **Resilience:** transient errors (`timeout`, `502`, `503`, `504`) are retried; everything else fails fast.
- **Fresh reads:** searches send `Cache-Control: no-cache`.

## 5. Traceability

| ID | Requirement (FHIR R4 spec) | Scenario | Status |
|---|---|---|---|
| FR-01 | Server publishes a CapabilityStatement with its FHIR version and interactions | The server declares FHIR R4 and the interactions under test | ✅ |
| FR-02 | Create returns `201`, `Location` with version and `ETag` | Register a new patient | ✅ |
| FR-03 | Read returns the stored resource | Register a new patient | ✅ |
| FR-04 | Update creates a new version (`meta.versionId`) | Updating a patient creates a new version… | ✅ |
| FR-05 | Previous versions are readable with vread | Updating a patient creates a new version… | ✅ |
| FR-06 | Reading a deleted resource returns `410 Gone` | A deleted patient is gone, not unknown | ✅ |
| FR-07 | Reading an unknown resource returns `404` | Reading a patient that never existed returns 404 | ✅ |
| FR-08 | Deleting a referenced resource is rejected (referential integrity) | A patient referenced by clinical data cannot be deleted | ✅ |
| FR-09 | Update body id must match the URL id | An update whose body id differs from the URL id is rejected | ✅ |
| FR-10 | Invalid content is rejected with `400` + OperationOutcome | Invalid gender code; non-FHIR date; wrong resource type | ✅ |
| FR-11 | Search by token (identifier) returns a `searchset` bundle with exact `total` | Find a patient by business identifier; unknown identifier | ✅ |
| FR-12 | Search by string + date parameters | Find a patient by family name and birth date | ✅ |
| FR-13 | Vital signs keep status, category, LOINC code, UCUM unit and subject | Record a vital sign (5 cases) | ✅ |
| FR-14 | Observations searchable by subject and code | Retrieve a patient's heart rate readings by LOINC code | ✅ |

**Coverage:** 14 requirements, 19 scenarios.

## 6. Risks

| Risk | Mitigation |
|---|---|
| Public server is shared, slow at times, occasionally unavailable | Retries for transient errors, generous time limit, weekday scheduled run |
| Server search cache hides newly created data | `Cache-Control: no-cache` on searches (see README finding) |
| Public server data is visible to anyone | Synthetic data only, no real patient information; cleanup after each scenario |

## 7. Entry and exit criteria

- **Entry:** `GET /metadata` returns `200` and declares FHIR 4.0.1.
- **Exit:** all `@smoke` scenarios pass; any `@regression` or `@negative` failure is analysed and either fixed or logged as a finding.
