using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Model; //Contiene los tipos de datos de los recursos FHIR.
using Hl7.Fhir.Rest; //Para llamadas HTTP y cliente FHIR.
using FHIR_CS_MobileSide_API.Models;

namespace FHIR_CS_MobileSide_API.Controllers
{
    [ApiController]
    [Route("MedAdm")]
    public class MedicationAdministrationController : ControllerBase
    {
        private readonly FhirClient _fhirClient;

        //Inicializar cliente FHIR a utilizar en los métodos de controlador.
        public MedicationAdministrationController(FHIRClient fhirClientService)
        {
            _fhirClient = fhirClientService.GetFhirClient();
        }


        /// <summary>
        /// Devuelve todos los recursos de MedicationAdministration de un paciente en particular de id patId.
        /// </summary>
        /// <param name="patId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("getAll")]
        public async Task<List<MedicationAdministration>> GetMedicationAdministrations(string patId)
        {
            List<MedicationAdministration> medAdmList = new List<MedicationAdministration>();
            SearchParams query = new SearchParams("subject", $"Patient/{patId}");
            Bundle medAdmBundle = await _fhirClient.SearchAsync<MedicationAdministration>(query);

            while (medAdmBundle != null)
            {
                foreach (Bundle.EntryComponent entry in medAdmBundle.Entry)
                {
                    if (entry.Resource != null)
                    {
                        MedicationAdministration medicationAdm = (MedicationAdministration)entry.Resource;
                        medAdmList.Add(medicationAdm);
                    }
                }
                medAdmBundle = await _fhirClient.ContinueAsync(medAdmBundle);
            }
            return medAdmList;
        }

        [HttpGet]
        [Route("getAllwithMeds")]
        public async Task<List<MedAdmBasicInfo>> GetMedicationAdministrationsAndMedications(string patRut)
        {
            List<MedicationAdministration> auxAdmList = new List<MedicationAdministration>();
            List<MedAdmBasicInfo> medAdmList = new List<MedAdmBasicInfo>();
            //Conseguir el recurso paciente con el rut que se le pasa. Luego obtener su id.
            SearchParams queryPat = new SearchParams("identifier", $"{patRut}");
            Bundle bundle= await _fhirClient.SearchAsync<Patient>(queryPat);
            Patient patientItem= (Patient) bundle.Entry.FirstOrDefault().Resource;

            SearchParams query = new SearchParams("subject", $"Patient/{patientItem.Id}");
            query.Include("MedicationAdministration:medication");
            Bundle medAdmBundle = await _fhirClient.SearchAsync<MedicationAdministration>(query);

            while (medAdmBundle != null)
            {
                foreach (Bundle.EntryComponent entry in medAdmBundle.Entry)
                {
                    if (entry.Resource != null)
                    {
                        if(entry.Resource.TypeName == "MedicationAdministration")
                        {
                            MedicationAdministration medicationAdm = (MedicationAdministration)entry.Resource;
                            //Fecha de ingesta:
                            DateTime medAdmDate= DateTime.ParseExact(medicationAdm.Effective.ToString().Substring(0,10),"yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                            string formattedDate = medAdmDate.ToString("dd-MM-yyyy");
                            //Hora de ingesta:
                            string formattedTime = medicationAdm.Effective.ToString().Substring(11, 5);
                            MedAdmBasicInfo item = new MedAdmBasicInfo
                            {
                                MedicationTime = "El " + formattedDate + " a las " + formattedTime + ".",
                                MedicationDose = medicationAdm.Dosage.Dose.Value.ToString() + " " + medicationAdm.Dosage.Dose.Unit  
                            };
                            auxAdmList.Add(medicationAdm);
                            medAdmList.Add(item);
                        }
                        else if(entry.Resource.TypeName == "Medication")
                        {
                            Medication medication = (Medication)entry.Resource;
                            for (int i = 0; i < auxAdmList.Count; i++)
                            {
                                ResourceReference medRef = (ResourceReference)auxAdmList[i].Medication;
                                if (medRef.Reference == $"Medication/{entry.Resource.Id}")
                                {
                                    medAdmList[i].MedicationName = medication.Code.Coding[0].Display;
                                    medAdmList[i].MedicationId = i.ToString();
                                }
                            }
                        }
                    }
                }
                medAdmBundle = await _fhirClient.ContinueAsync(medAdmBundle);
            }
            return medAdmList;
        }

        [HttpPost]
        [Route("create")]
        public async Task<dynamic> CreateMedicationAdministration([FromBody] MedicationAdministrationClass medAdmClass)
            //El Id y nombre del paciente se encuentra en los datos de paciente en la aplicación móvil y son entregados  
             //como parámetros durante la llamada API.
        {
            Patient patient= await _fhirClient.ReadAsync<Patient>($"Patient/{medAdmClass.patientId}");
            MedicationAdministration toCreate = new MedicationAdministration()
            {
                Status = MedicationAdministration.MedicationAdministrationStatusCodes.Completed, //estado de la administración de medicamento

                Medication = new ResourceReference() //medicacion administrada
                {
                    Reference = $"Medication/{medAdmClass.medicationId}"
                },
                Subject = new ResourceReference() //paciente al cual se administró
                {
                    Reference = $"Patient/{medAdmClass.patientId}",
                    Display = patient.Name[0].GivenElement[0].Value + " " + patient.Name[0].FamilyElement.Value
                },
                Effective = new FhirDateTime(medAdmClass.timestamp),
                Dosage = new MedicationAdministration.DosageComponent()
                {
                    Text = "0.05 - 0.1mg/kg IV over 2-5 minutes every 15 minutes as needed", //Instrucciones de médico en texto plano
                    Route = new CodeableConcept() //Parte del cuerpo a donde administrar.
                    {
                        Coding = new List<Coding>()
                        {
                            new Coding()
                            {
                                System= "http://snomed.info/sct",
                                Code= "255560000",
                                Display= "Intravenous"
                            }
                        }
                    },
                    Method = new CodeableConcept() //Técnica para administrar la medicación.
                    {
                        Coding = new List<Coding>()
                        {
                            new Coding()
                            {
                                System= "http://snomed.info/sct",
                                Code= "420620005",
                                Display= "Push - dosing instruction imperative (qualifier value)"
                            }
                        }
                    },
                    Dose = new Quantity() //Cantidad de medicamento por dosis (en este caso 7mg)
                    {
                        Value = medAdmClass.ingestedDoseQuantity,
                        Unit = medAdmClass.ingestedDoseUnit,
                        System = "http://unitsofmeasure.org",
                        Code = medAdmClass.ingestedDoseUnit
                    },
                    Rate = new Quantity() //Cantidad de medicamento por unidad de tiempo (en este caso 4min).
                    {
                        Value = 4,
                        Unit = "min",
                        System = "http://unitsofmeasure.org",
                        Code = "min"
                    }
                }
            };
            MedicationAdministration created = await _fhirClient.CreateAsync<MedicationAdministration>(toCreate);
            //ResourceReference referencia_medicina = (ResourceReference) created.Medication;
            //Medication nombre_medicina= await _fhirClient.ReadAsync<Medication>($"Medication/{medId}");
            return new
            {
                success = true,
                message = "MedicationAdministration registrada",
                result = created
            };
        }

        [HttpGet]
        [Route("readById")]
        public async Task<MedicationAdministration> ReadMedicationAdministration(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            return await _fhirClient.ReadAsync<MedicationAdministration>($"MedicationAdministration/{id}");
        }



        [HttpPut]
        [Route("updateById")]
        public async Task<dynamic> UpdateMedicationAdministration(
            string id,
            [FromBody] MedicationAdministrationClass medAdmClass
        )
        {
            MedicationAdministration toUpdate = await _fhirClient.ReadAsync<MedicationAdministration>($"MedicationAdministration/{id}");
            if (medAdmClass.medicationId != null)
            {
                ResourceReference medRef = new ResourceReference() //medicacion administrada
                {
                    Reference = $"Medication/{medAdmClass.medicationId}"
                };
                toUpdate.Medication = medRef;
            }

            MedicationAdministration updated = await _fhirClient.UpdateAsync<MedicationAdministration>(toUpdate);
            return new
            {
                success = true,
                message = "MedicationAdministration actualizada",
                result = updated
            };
        }



        [HttpDelete]
        [Route("delete")]
        public async Task<dynamic> DeleteMedicationadministration(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            await _fhirClient.DeleteAsync($"MedicationAdministration/{id}");
            return new
            {
                success = true,
                message = "MedicationAdministration eliminada",
                id= id
            };
        }
    }
}