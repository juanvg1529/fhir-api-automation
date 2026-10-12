using Hl7.Fhir.Model;

namespace Fhir.ApiTests.Support;

public static class FhirSystems
{
    /// <summary>Identifier namespace owned by this test suite.</summary>
    public const string PortfolioIdentifier = "urn:jdvg:fhir-portfolio";
    public const string Loinc = "http://loinc.org";
    public const string Ucum = "http://unitsofmeasure.org";
    public const string ObservationCategory = "http://terminology.hl7.org/CodeSystem/observation-category";
}

public static class PatientBuilder
{
    public static Patient Create(string identifierValue, string family, string given, string gender, string birthDate) => new()
    {
        Active = true,
        Identifier = [new Identifier(FhirSystems.PortfolioIdentifier, identifierValue)],
        Name = [new HumanName { Use = HumanName.NameUse.Official, Family = family, Given = given.Split(' ') }],
        Gender = ParseGender(gender),
        BirthDate = birthDate
    };

    public static Patient Default(string identifierValue) =>
        Create(identifierValue, "Vera", "Juan Diego", "male", "1990-05-15");

    private static AdministrativeGender ParseGender(string gender) => gender.ToLowerInvariant() switch
    {
        "male" => AdministrativeGender.Male,
        "female" => AdministrativeGender.Female,
        "other" => AdministrativeGender.Other,
        "unknown" => AdministrativeGender.Unknown,
        _ => throw new ArgumentException($"'{gender}' is not an AdministrativeGender code", nameof(gender))
    };
}

public static class ObservationBuilder
{
    /// <summary>A vital-sign Observation following the FHIR R4 vital signs profile basics.</summary>
    public static Observation VitalSign(string patientId, string loinc, string display, decimal value, string ucumUnit) => new()
    {
        Status = ObservationStatus.Final,
        Category = [new CodeableConcept(FhirSystems.ObservationCategory, "vital-signs", "Vital Signs")],
        Code = new CodeableConcept(FhirSystems.Loinc, loinc, display),
        Subject = new ResourceReference($"Patient/{patientId}"),
        Effective = new FhirDateTime(DateTimeOffset.UtcNow),
        Value = new Quantity(value, ucumUnit, FhirSystems.Ucum)
    };
}
