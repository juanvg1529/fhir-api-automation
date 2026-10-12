Feature: Input validation and error handling
  Invalid requests must be rejected with a 4xx status and an
  OperationOutcome that explains the problem.

  @negative
  Scenario: An invalid gender code is rejected
    When I submit this "Patient":
      """json
      { "resourceType": "Patient", "gender": "robot" }
      """
    Then the response status code should be 400
    And the response should be an OperationOutcome with an error mentioning "gender"

  @negative
  Scenario: A birth date in a non-FHIR format is rejected
    When I submit this "Patient":
      """json
      { "resourceType": "Patient", "birthDate": "15/05/1990" }
      """
    Then the response status code should be 400
    And the response should be an OperationOutcome with an error mentioning "15/05/1990"

  @negative
  Scenario: A resource posted to the wrong endpoint is rejected
    When I submit this "Patient":
      """json
      { "resourceType": "Observation", "status": "final", "code": { "text": "Heart rate" } }
      """
    Then the response status code should be 400
    And the response should be an OperationOutcome with an error mentioning "Incorrect resource type"

  @negative
  Scenario: An update whose body id differs from the URL id is rejected
    Given a registered patient
    When I update the patient sending a different id in the body
    Then the response status code should be 400
    And the response should be an OperationOutcome with an error mentioning "does not match"

  @negative
  Scenario: Reading a patient that never existed returns 404
    When I read a patient that does not exist
    Then the response status code should be 404
    And the response should be an OperationOutcome with an error mentioning "not known"
