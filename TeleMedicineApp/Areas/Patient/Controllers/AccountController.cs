using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeleMedicineApp.Areas.Admin.Provider;
using TeleMedicineApp.Areas.Doctor.Models;
using TeleMedicineApp.Areas.Doctor.ViewModels;
using TeleMedicineApp.Areas.Patient.Models;
using TeleMedicineApp.Areas.Patient.ViewModels;
using TeleMedicineApp.Controllers;
using TeleMedicineApp.Data;

namespace TeleMedicineApp.Areas.Patients.Controllers;

[Authorize(Roles = "SuperAdmin,Patient")] // Default authorization for all actions
[Area("Patient")]
[Route("api/[area]/[action]")]
[ApiController]

public class PatientController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger _logger;
    private readonly ApplicationDbContext _context;
    private readonly PatientManager _patientManager;


    public PatientController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILoggerFactory loggerFactory,
        PatientManager patientManager)

    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = loggerFactory.CreateLogger<PatientController>();
        _patientManager = patientManager;

    }
    [HttpPost]
[AllowAnonymous]
[Consumes("multipart/form-data")]
public async Task<IActionResult> RegisterPatient([FromForm] RegisterPatientDTO dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    _logger.LogInformation("📥 Registering new patient: {FullName}, Phone: {PhoneNumber}", dto.FullName, dto.PhoneNumber);

    string imagePath = null;
    if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
    {
        var ext = Path.GetExtension(dto.ProfileImage.FileName).ToLower();
        var allowed = new[] { ".jpg", ".jpeg", ".png" };

        if (!allowed.Contains(ext))
            return BadRequest("Only jpg, jpeg, and png formats are allowed.");

        if (dto.ProfileImage.Length > 2 * 1024 * 1024)
            return BadRequest("Max image size is 2MB.");

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "patients");
        Directory.CreateDirectory(uploadDir);

        var fileName = Guid.NewGuid().ToString() + ext;
        var filePath = Path.Combine(uploadDir, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await dto.ProfileImage.CopyToAsync(stream);

        imagePath = "/uploads/patients/" + fileName;
    }

    var patient = new PatientDetails
    {
        UserId = dto.UserId,
        FullName = dto.FullName,
        PhoneNumber = dto.PhoneNumber,
        Gender = dto.Gender,
        DateOfBirth = dto.DateOfBirth,
        BloodGroup = dto.BloodGroup,
        Address = dto.Address,
        EmergencyContactName = dto.EmergencyContactName,
        EmergencyContactNumber = dto.EmergencyContactNumber,
        HealthInsuranceProvider = dto.HealthInsuranceProvider,
        MedicalHistory = dto.MedicalHistory,
        ProfileImage = imagePath,
        MaritalStatus = dto.MaritalStatus,
        Allergies = dto.Allergies,
        ChronicDiseases = dto.ChronicDiseases,
        Medications = dto.Medications,
        Status = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    _context.PatientDetails.Add(patient);
    await _context.SaveChangesAsync();

    return Ok("Patient registered successfully.");
}

    // Helper function for password validation
    private bool IsValidPassword(string password)
    {
        // The password must contain at least one uppercase letter, one lowercase letter,
        // one digit, one special character (@$!%*?&) and be at least 8 characters long.
        var pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";
        return Regex.IsMatch(password, pattern);
    }
    // Get all patients with pagination, filtering, and sorting
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPatients(
        string search = "", 
        string sortColumn = "CreatedAt", 
        string sortOrder = "ASC", 
        int page = 1, 
        int pageSize = 5)
    {
        try
        {
            // Calculate offset for pagination
            int offset = (page - 1) * pageSize;

            // Fetch paginated list of patients
            var patients = await _patientManager.GetAllPatients(offset, pageSize, search, sortColumn, sortOrder);

            if (patients == null || !patients.Any())
            {
                return NotFound("There are no patients.");
            }

            return Ok(new
            {
                currentPage = page,
                pageSize = pageSize,
                patients
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPatients");
            return StatusCode(500, "An error occurred while fetching patient data.");
        }
    }
        // Get a specific patient by ID
        [HttpGet("{patientId}")]
        public async Task<PatientUpdateViewModel> GetPatientById(int patientId)
        {
            // Assuming you're using Entity Framework to fetch data
            var patient = await _context.PatientDetails
                .Where(p => p.PatientId == patientId)
                .Include(p => p.User)  // Joining AspNetUsers (User table)
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return null;
            }

            // Mapping to ViewModel
            return new PatientUpdateViewModel
            {
                PatientId = patient.PatientId,
                FullName = patient.FullName,
                PhoneNumber = patient.PhoneNumber,
                Email = patient.User.Email,  // Mapping email from AspNetUsers
                Gender = patient.Gender,
                DateOfBirth = patient.DateOfBirth,
                BloodGroup = patient.BloodGroup,
                Address = patient.Address,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactNumber = patient.EmergencyContactNumber,
                HealthInsuranceProvider = patient.HealthInsuranceProvider,
                MedicalHistory = patient.MedicalHistory,
                ProfileImage = patient.ProfileImage,
                MaritalStatus = patient.MaritalStatus,
                Allergies = patient.Allergies,
                ChronicDiseases = patient.ChronicDiseases,
                Medications = patient.Medications,
                Status = patient.Status
            };
        }
        // Delete a patient by userId
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeletePatient(string userId)
        {
            try
            {
                var response = await _patientManager.DeletePatientByUserId(userId);
        
                if (response.IsSuccess)
                {
                    return Ok(new { message = response.Result });
                }
                else
                {
                    return BadRequest(new { message = response.Result, error = response.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error deleting patient", details = ex.Message });
            }
        }

        [HttpPut("{patientId}")]
        public async Task<IActionResult> UpdatePatient(int patientId, [FromBody] PatientUpdateViewModel model)
        {
            try
            {
                var patient = await _patientManager.GetPatientById(patientId);

                if (patient == null)
                {
                    return BadRequest(new
                    {
                        message = "Error updating patient details",
                        details = "Patient with the given ID does not exist."
                    });
                }

                var isUpdated = await _patientManager.UpdatePatientDetails(model, patientId);

                if (isUpdated)
                {
                    return Ok(new { message = "Doctor updated successfully." });
                }

                return StatusCode(500,
                    new { message = "Update executed, but no rows were modified. This could be because the" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error updating patient details", details = ex.Message });
            }
        }
        // Get a patient's appointments
        [HttpGet("{patientId}")]
        public async Task<IActionResult> GetPatientAppointments(int patientId, int offset, int limit)
        {
            try
            {
                var result = await _patientManager.GetPatientAppointments(patientId, offset, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error retrieving patient appointments", details = ex.Message });
            }
        }

        // Get userId by patientId
        [HttpGet("{patientId}")]
        public async Task<IActionResult> GetUserIdByPatientId(int patientId)
        {
            try
            {
                var response = await _patientManager.GetUserIdByPatientId(patientId);
                if (response.Result.StartsWith("Error"))
                {
                    return BadRequest(response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error retrieving userId for patient", details = ex.Message });
            }
        }
        [HttpPut("{patientId}")]
        public async Task<IActionResult> TogglePatientStatus(int patientId)
        {
            var patient = await _context.PatientDetails.FindAsync(patientId);
            if (patient == null)
                return NotFound("Patient not found");

            patient.Status = !patient.Status;
            patient.UpdatedAt = DateTime.UtcNow;

            _context.PatientDetails.Update(patient);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Patient status toggled successfully", isActive = patient.Status });
        }
}