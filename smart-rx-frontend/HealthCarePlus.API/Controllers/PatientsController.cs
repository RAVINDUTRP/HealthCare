using Microsoft.AspNetCore.Mvc;
using HealthCarePlus.API.Models;
using HealthCarePlus.API.Services;

namespace HealthCarePlus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly PatientService _patientService;

    public PatientsController(PatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPatients()
    {
        var patients = await _patientService.GetAllPatientsAsync();
        return Ok(patients);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatientById(string id)
    {
        var patient = await _patientService.GetPatientByIdAsync(id);
        if (patient == null)
            return NotFound("Patient not found");

        return Ok(patient);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePatient(string id, [FromBody] Patient patient)
    {
        if (id != patient.Id)
            return BadRequest("Patient ID mismatch");

        var result = await _patientService.UpdatePatientAsync(id, patient);
        if (!result)
            return NotFound("Patient not found");

        return Ok(new { message = "Patient updated successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePatient(string id)
    {
        var result = await _patientService.DeletePatientAsync(id);
        if (!result)
            return NotFound("Patient not found");

        return Ok(new { message = "Patient deleted successfully" });
    }
}