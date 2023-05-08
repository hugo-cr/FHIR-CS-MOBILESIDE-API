

using Hl7.Fhir.Rest;

public class FHIRClient
{
    private readonly FhirClient _fhirClient;

    public FHIRClient(string url)
    {
        _fhirClient = new FhirClient(url);
    }

    public FhirClient GetFhirClient()
    {
        return _fhirClient;
    }
}