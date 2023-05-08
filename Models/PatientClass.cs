namespace FHIR_CS_MobileSide_API.Models
{
    public class PatientClass
    {
        public bool? active { get; set; }

        public string? identifier { get; set; }
        public string? familyName { get; set; }
        public string? givenName { get; set; }
        public string? gender { get; set; }
        public string? birthDate { get; set; }
        public string? address { get; set; }
    }
}