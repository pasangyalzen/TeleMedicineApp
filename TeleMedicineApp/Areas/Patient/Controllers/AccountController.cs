using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeleMedicineApp.Areas.Admin.Provider;
using TeleMedicineApp.Areas.Doctor.Models;
using TeleMedicineApp.Areas.Doctor.ViewModels;
using TeleMedicineApp.Areas.Patient.Models;
using TeleMedicineApp.Controllers;
using TeleMedicineApp.Data;

namespace TeleMedicineApp.Areas.Patients.Controllers;

[Authorize(Roles = "SuperAdmin")] // Default authorization for all actions
[Area("Doctor")]
[Route("api/[area]/[action]")]
[ApiController]

public class AccountController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger _logger;
    private readonly ApplicationDbContext _context;
    private readonly PatientManager _patientManager;


    public AccountController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILoggerFactory loggerFactory,
        PatientManager patientManager)

    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = loggerFactory.CreateLogger<AccountController>();
        _patientManager = patientManager;

    }
    [HttpPost("register-patient")]
    public async Task<IActionResult> RegisterPatient([FromBody] RegisterPatientDTO registerPatientDTO)
    {
        // Validate passwords match
        if (registerPatientDTO.Password != registerPatientDTO.ConfirmPassword)
        {
            return BadRequest("Passwords do not match.");
        }

        // Check if email exists
        var existingUser = await _userManager.FindByEmailAsync(registerPatientDTO.Email);
        if (existingUser != null)
        {
            return BadRequest("Email is already in use.");
        }

        // Create User in AspNetUsers
        var user = new ApplicationUser
        {
            UserName = registerPatientDTO.Email,
            Email = registerPatientDTO.Email
        };

        var result = await _userManager.CreateAsync(user, registerPatientDTO.Password);
        if (!result.Succeeded)
        {
            return BadRequest("User registration failed.");
        }

        // Add User to PatientDetails table
        var patientDetails = new PatientDetails
        {
            UserId = user.Id,
            FullName = registerPatientDTO.FullName,
            PhoneNumber = registerPatientDTO.PhoneNumber,
            Gender = registerPatientDTO.Gender,
            DateOfBirth = registerPatientDTO.DateOfBirth,
            BloodGroup = registerPatientDTO.BloodGroup,
            Address = registerPatientDTO.Address,
            EmergencyContactName = registerPatientDTO.EmergencyContactName,
            EmergencyContactNumber = registerPatientDTO.EmergencyContactNumber,
            HealthInsuranceProvider = registerPatientDTO.HealthInsuranceProvider,
            MedicalHistory = registerPatientDTO.MedicalHistory,
            ProfileImage = registerPatientDTO.ProfileImage,
            MaritalStatus = registerPatientDTO.MaritalStatus,
            Allergies = registerPatientDTO.Allergies,
            ChronicDiseases = registerPatientDTO.ChronicDiseases,
            Medications = registerPatientDTO.Medications,
            Status = true, // Active by default
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PatientDetails.Add(patientDetails);
        await _context.SaveChangesAsync();

        return Ok("Patient registered successfully.");
    }
    // Get all patients with pagination, filtering, and sorting
    [HttpGet("GetAllPatients")]
    public async Task<IActionResult> GetAllPatients(string search = "", string sortColumn = "CreatedAt", string sortOrder = "ASC", int page = 1, int pageSize = 5)
    {
        try
        {
            // Calculate offset based on the current page and page size
            var offset = (page - 1) * pageSize;

            // Fetch patients using the _patientManager with pagination and sorting
            var patients = await _patientManager.GetAllPatients(offset, pageSize, search, sortColumn, sortOrder);

            // Check if no patients were found
            if (patients == null || !patients.Any())
            {
                return NotFound("There are no patients.");
            }

            // Return the patients data as a response
            return Ok(patients);
        }
        catch (Exception ex)
        {
            // Log any errors that occur
            _logger.LogError(ex, "Error in GetAllPatients");
            return StatusCode(500, new { message = "Error retrieving patients", details = ex.Message });
        }
    }
        // Get a specific patient by ID
        [HttpGet("GetPatientById/{patientId}")]
        public async Task<IActionResult> GetPatientById(int patientId)
        {
            try
            {
                var result = await _patientManager.GetPatientById(patientId);
                if (result == null)
                {
                    return NotFound(new { message = "Patient not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error retrieving patient details", details = ex.Message });
            }
        }

        // Delete a patient by userId
        [HttpDelete("DeletePatient/{userId}")]
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

        // Update patient details
        [HttpPut("UpdatePatient/{patientId}")]
        public async Task<IActionResult> UpdatePatient(int patientId, [FromBody] PatientUpdateViewModel model)
        {
            try
            {
                var isUpdated = await _patientManager.UpdatePatient(model, patientId);
                if (!isUpdated)
                {
                    return BadRequest(new { message = "Error updating patient details" });
                }
                return Ok(new { message = "Patient details updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error updating patient details", details = ex.Message });
            }
        }

        // Get a patient's appointments
        [HttpGet("GetPatientAppointments/{patientId}")]
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
        [HttpGet("GetUserIdByPatientId/{patientId}")]
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
}