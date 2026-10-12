Feature: Server capabilities
  Before testing anything else, confirm the server speaks FHIR R4 and
  supports the interactions this suite depends on (GET /metadata).

  @smoke
  Scenario: The server declares FHIR R4 and the interactions under test
    When I request the server's capability statement
    Then the response status code should be 200
    And the response time should be within the agreed limit
    And the server should declare FHIR version "4.0.1"
    And the server should support these interactions:
      | resource    | interactions                                    |
      | Patient     | create, read, vread, update, delete, search-type |
      | Observation | create, read, delete, search-type               |
