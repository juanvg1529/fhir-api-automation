Feature: Vital signs
  Nurses record vital signs as Observations coded with LOINC and
  measured in UCUM units, linked to the patient.

  Background:
    Given a registered patient

  @smoke
  Scenario Outline: Record a vital sign - <vital>
    When I record a "<vital>" observation with LOINC "<loinc>" and value <value> "<unit>"
    Then the response status code should be 201
    And the response time should be within the agreed limit
    And the stored observation should be a final vital sign for the patient
    And the stored observation should have LOINC "<loinc>" and value <value> "<unit>"

    Examples:
      | vital             | loinc   | value | unit |
      | Heart rate        | 8867-4  | 72    | /min |
      | Body temperature  | 8310-5  | 36.8  | Cel  |
      | Respiratory rate  | 9279-1  | 16    | /min |
      | Body weight       | 29463-7 | 70.5  | kg   |
      | Oxygen saturation | 2708-6  | 98    | %    |

  @regression
  Scenario: Retrieve a patient's heart rate readings by LOINC code
    Given the patient has a heart rate observation of 72 "/min"
    When I search the patient's observations with LOINC "8867-4"
    Then the response status code should be 200
    And the search should return a "searchset" bundle with 1 match
