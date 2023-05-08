using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Model; //Contiene los tipos de datos de los recursos FHIR.
using Hl7.Fhir.Rest; //Para llamadas HTTP y cliente FHIR.
using FHIR_CS_MobileSide_API.Models;

namespace FHIR_CS_MobileSide_API.Controllers
{
    [ApiController]
    [Route("Med")]
    public class MedicationController : ControllerBase
    {   
        private readonly FhirClient _fhirClient;

        //Inicializar cliente FHIR a utilizar en los métodos de controlador.
        public MedicationController(FHIRClient fhirClientService)
        {
            _fhirClient = fhirClientService.GetFhirClient();
        }

        [HttpGet]
        [Route("getAll")]
        public async Task<Bundle> GetMedications(

        )
        {
            List<Medication> medList = new List<Medication>();
            Bundle medBundle= await _fhirClient.SearchAsync<Medication>();
            //if(query== null){
                medBundle = await _fhirClient.SearchAsync<Medication>();
            //}
            // else //Casos donde la QUERY REST incluya algún criterio de búsqueda.

            // {
            //     if(query.resourceOrigin == "MedicationAdministration")
            //     {
            //         var searchParams = new SearchParams();
            //         searchParams.Add("_id", string.Join(",", query.medicationCriteria));
            //         //searchParams.Add("_has","MedicationAdministration:medication");
            //         searchParams.Add("_include", "MedicationAdministration:medication");
            //         medBundle = await _fhirClient.SearchAsync<Medication>(searchParams);
            //     }
            // }
            

            while (medBundle != null)
            {
                foreach (Bundle.EntryComponent entry in medBundle.Entry)
                {
                    if (entry.Resource != null)
                    {
                        Medication medication = (Medication)entry.Resource;
                        medList.Add(medication);
                    }
                }
                medBundle = await _fhirClient.ContinueAsync(medBundle);
            }
            return medBundle;
            //return query.medicationCriteria;
        }

        [HttpGet]
        [Route("readById/{id}")]
        public async Task<Medication> ReadMedication(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            return await _fhirClient.ReadAsync<Medication>($"Medication/{id}");;
        }
    }

}
