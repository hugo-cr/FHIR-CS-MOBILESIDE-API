namespace FHIR_CS_MobileSide_API.Models
{
    public class MedicationAdministrationClass
    {
        public string? medicationId { get; set; }
        public string? patientId { get; set; }
        public string? timestamp { get; set; }
        public int? ingestedDoseQuantity { get; set; }
        public string? ingestedDoseUnit { get; set; }

    }

    public class MedAdmBasicInfo {
        public string MedicationName { get; set; }
        public string MedicationTime { get; set; }
        public string MedicationDose { get; set; }
        public string MedicationId { get; set; }
    }
}