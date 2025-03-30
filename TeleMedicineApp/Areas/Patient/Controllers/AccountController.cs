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

[Authorize(Roles = "SuperAdmin")] // Default authorization for all actions
[Area("Patient")]
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
    [HttpPost]
    public async Task<IActionResult> RegisterPatient([FromBody] RegisterPatientDTO registerPatientDTO)
    {
        // Validate ModelState (Ensures all required fields and custom validations are checked)
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate passwords match
        if (registerPatientDTO.Password != registerPatientDTO.ConfirmPassword)
        {
            return BadRequest("Passwords do not match.");
        }

        // Validate Password Format (custom validation logic)
        if (!IsValidPassword(registerPatientDTO.Password))
        {
            return BadRequest("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character (@$!%*?&), and must be at least 8 characters long.");
        }

        // Check if email exists
        var existingUser = await _userManager.FindByEmailAsync(registerPatientDTO.Email);
        if (existingUser != null)
        {
            return BadRequest("Email is already in use.");
        }

        // Validate Date of Birth (Check if the patient is at least 18 years old)
        if (registerPatientDTO.DateOfBirth > DateTime.UtcNow.AddYears(-18))
        {
            return BadRequest("Patient must be at least 18 years old.");
        }

        // Check if ProfileImage is valid (Optional based on your use case)
        if (!Uri.IsWellFormedUriString(registerPatientDTO.ProfileImage, UriKind.Absolute))
        {
            return BadRequest("Profile image URL is not valid.");
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
        var roleResult = await _userManager.AddToRoleAsync(user, "Patient");
        if (!roleResult.Succeeded)
        {
            return BadRequest("Failed to assign role.");
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
}