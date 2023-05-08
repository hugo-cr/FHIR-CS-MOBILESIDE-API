namespace FHIR_CS_MobileSide_API.Models
{
    public class MedicationClass
    {
        public string? medicationId { get; set; }
    }

    public class medicationQueryData
    {
        public string resourceOrigin { get; set; }
        
        public string[] medicationCriteria { get; set;}
    }
}