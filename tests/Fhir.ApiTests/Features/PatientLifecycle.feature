Feature: Patient lifecycle
  As a clinical system
  I want to register, update and remove patients
  So that demographic data stays correct and every change is traceable

  @smoke
  Scenario: Register a new patient
    Given a new patient:
      | family | given      | gender | birthDate  |
      | Vera   | Juan Diego | male   | 1990-05-15 |
    When I register the patient
    Then the response status code should be 201
    And the response time should be within the agreed limit
    And the server should assign an id and version "1"
    And the Location header should point to version "1" of the patient
    And reading the patient should return the same demographics

  @regression
  Scenario: Updating a patient creates a new version and keeps the history
    Given a registered patient
    When I change the patient's given name to "Juan"
    Then the response status code should be 200
    And the server should report version "2"
    And reading the patient should return the same demographics
    And version "1" of the patient should keep the original given name

  @regression
  Scenario: A deleted patient is gone, not unknown
    Given a registered patient
    When I delete the patient
    Then the response status code should be 200
    # FHIR distinguishes 410 Gone (existed, then deleted) from 404 Not Found (never existed).
    And reading the patient should return status 410

  @negative
  Scenario: A patient referenced by clinical data cannot be deleted
    Given a registered patient
    And the patient has a heart rate observation of 72 "/min"
    When I delete the patient
    # Referential integrity: deleting would leave the observation pointing at nothing.
    Then the response status code should be 409
    And the response should be an OperationOutcome with an error mentioning "reference"
    And reading the patient should return status 200
