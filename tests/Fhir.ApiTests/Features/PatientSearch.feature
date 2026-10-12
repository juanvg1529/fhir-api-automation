Feature: Patient search
  Clinicians find patients by business identifier or demographics.

  @regression
  Scenario: Find a patient by business identifier
    Given a registered patient
    When I search patients by the patient's identifier
    Then the response status code should be 200
    And the search should return a "searchset" bundle with 1 match
    And the bundle should contain the patient

  @regression
  Scenario: Find a patient by family name and birth date
    Given a registered patient
    When I search patients by the patient's family name and birth date
    Then the response status code should be 200
    And the bundle should contain the patient

  @regression
  Scenario: Searching for an unknown identifier returns an empty bundle
    When I search patients by an identifier that does not exist
    Then the response status code should be 200
    And the search should return a "searchset" bundle with 0 matches
