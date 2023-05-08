using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Model; //Contiene los tipos de datos de los recursos FHIR.
using Hl7.Fhir.Rest; //Para llamadas HTTP y cliente FHIR.
using FHIR_CS_MobileSide_API.Models;

namespace FHIR_CS_MobileSide_API.Controllers
{
    [ApiController]
    [Route("Pat")]
    public class PatientController : ControllerBase
    {
        private readonly FhirClient _fhirClient;
    
        //Inicializar cliente FHIR a utilizar en los métodos de controlador.
        public PatientController(FHIRClient fhirClientService)
        {
            _fhirClient = fhirClientService.GetFhirClient();
        }


        /// <summary>
        /// Devuelve todos los recursos de pacientes.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("getAll")]
        public async Task<List<Patient>> GetPatients()
        {
            List<Patient> patList = new List<Patient>();
            Bundle patBundle = await _fhirClient.SearchAsync<Patient>();

            while (patBundle != null)
            {
                foreach (Bundle.EntryComponent entry in patBundle.Entry)
                {
                    if (entry.Resource != null)
                    {
                        Patient patient = (Patient)entry.Resource;
                        patList.Add(patient);
                    }
                }
                patBundle = await _fhirClient.ContinueAsync(patBundle);
            }
            return patList;
        }

        /// <summary>
        /// Crea un recurso de paciente con los parámetros indicados
        /// </summary>
        /// <param name="patientClass"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("create")]
        public async Task<dynamic> CreatePatient([FromBody] PatientClass patientClass)
        {
            AdministrativeGender realGender;
            switch (patientClass.gender)
            {
                case "male":
                case "Male":
                    realGender = AdministrativeGender.Male;
                    break;
                case "female":
                case "Female":
                    realGender = AdministrativeGender.Female;
                    break;
                case "other":
                case "Other":
                    realGender = AdministrativeGender.Other;
                    break;
                default:
                    realGender = AdministrativeGender.Unknown;
                    break;
            }

            Patient toCreate = new Patient()
            {
                Active = patientClass.active, //Ficha de paciente en uso.

                Name = new List<HumanName>()
                {
                    new HumanName()
                    {
                        Family = patientClass.familyName,
                        Given = new List<string>()
                        {
                            patientClass.givenName
                        }
                    }
                },
                Gender = realGender,
                BirthDateElement = new Date(patientClass.birthDate),
                Address = new List<Address>()
                {
                    new Address()
                    {
                        Text= patientClass.address
                    }
                }

            };
            Patient created = await _fhirClient.CreateAsync<Patient>(toCreate);
            //ResourceReference referencia_medicina = (ResourceReference) created.Medication;
            //Medication nombre_medicina= await _fhirClient.ReadAsync<Medication>($"Medication/{medId}");
            return new
            {
                success = true,
                message = "Patient registrado",
                result = created
            };
        }


        /// <summary>
        /// Read a patient by its rut.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        [HttpGet]
        [Route("readById")]
        public async Task<Patient> ReadPatient(string rut)
        {
            if (string.IsNullOrEmpty(rut))
            {
                throw new ArgumentNullException(nameof(rut));
            }
            SearchParams query = new SearchParams("identifier", $"{rut}");
            Bundle bundle= await _fhirClient.SearchAsync<Patient>(query);
            return (Patient) bundle.Entry.FirstOrDefault().Resource;
        }


        /// <summary>
        /// Update a patient given its Id with the passed parameters.
        /// </summary>
        /// <param name="active"></param>
        /// <param name="familyName"></param>
        /// <param name="givenName"></param>
        /// <param name="gender"></param>
        /// <param name="birthDate"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("updateById")]
        public async Task<dynamic> UpdatePatient(
            string id,
            [FromBody] PatientClass patientClass
        )
        {
            Patient toUpdate = await _fhirClient.ReadAsync<Patient>($"Patient/{id}");
 
            //Activo
            if (patientClass.active != null)
            {
                toUpdate.Active = patientClass.active;
            }

            //Identifier o rut.
            if(patientClass.identifier != null)
            {
                //Quitar puntos a rut.
                string modifiedIdentifier = patientClass.identifier.Replace(".", "");
                List<Identifier> identifierList = new List<Identifier>()
                {
                    new Identifier()
                    {
                        Value = modifiedIdentifier
                    }
                };
                toUpdate.Identifier = identifierList;
            }
            //Nombre.
            if (!string.IsNullOrEmpty(patientClass.familyName) && !string.IsNullOrEmpty(patientClass.givenName))
            {
                List<HumanName> newName = new List<HumanName>()
                {
                    new HumanName()
                    {
                        Family = patientClass.familyName,
                        Given = new List<string>()
                        {
                            patientClass.givenName
                        }
                    }
                };
                toUpdate.Name = newName;
            }

            //Género
            if (!string.IsNullOrEmpty(patientClass.gender))
            {
                AdministrativeGender realGender;
                switch (patientClass.gender)
                {
                    case "male":
                    case "Male":
                        realGender = AdministrativeGender.Male;
                        break;
                    case "female":
                    case "Female":
                        realGender = AdministrativeGender.Female;
                        break;
                    case "other":
                    case "Other":
                        realGender = AdministrativeGender.Other;
                        break;
                    default:
                        realGender = AdministrativeGender.Unknown;
                        break;
                }
                toUpdate.Gender = realGender;
            }

            //Cumpleaños
            if (!string.IsNullOrEmpty(patientClass.birthDate))
            {
                toUpdate.BirthDateElement = new Date(patientClass.birthDate);
            }

            //Dirección
            if (!string.IsNullOrEmpty(patientClass.address))
            {
                List<Address> newAddress = new List<Address>()
                {
                    new Address()
                    {
                        Text= patientClass.address
                    }
                };
                toUpdate.Address = newAddress;
            }

           
            Patient updated = await _fhirClient.UpdateAsync<Patient>(toUpdate);
            return new
            {
                success = true,
                message = "Patient actualizado",
                result = updated
            };
        }


        /// <summary>
        /// Delete a Patient resource by its id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        [HttpDelete]
        [Route("delete")]
        public async Task<dynamic> DeletePatient(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            await _fhirClient.DeleteAsync($"Patient/{id}");
            return new
            {
                success = true,
                message = "Patient eliminado",
                id = id
            };
        }
    }
}