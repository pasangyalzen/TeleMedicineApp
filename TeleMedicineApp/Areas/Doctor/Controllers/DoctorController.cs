using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeleMedicineApp.Areas.Doctor.Models;
using TeleMedicineApp.Areas.Doctor.ViewModels;
using TeleMedicineApp.Controllers;
using TeleMedicineApp.Data;

namespace TeleMedicineApp.Areas.Doctor.Controllers;

[Authorize(Roles = "SuperAdmin")] // Default authorization for all actions
[Area("Doctor")]
[Route("api/[area]/[action]")]
[ApiController]

public class DoctorController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger _logger;
    private readonly ApplicationDbContext _context;


    public DoctorController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILoggerFactory loggerFactory)

    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = loggerFactory.CreateLogger<DoctorController>();

    }

    // Register Doctor
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterDoctor([FromBody] RegisterDoctorDTO registerDoctorDTO)
    {
        // Validate passwords match
        if (registerDoctorDTO.Password != registerDoctorDTO.ConfirmPassword)
        {
            return BadRequest("Passwords do not match.");
        }

        // Check if email exists
        var existingUser = await _userManager.FindByEmailAsync(registerDoctorDTO.Email);
        if (existingUser != null)
        {
            return BadRequest("Email is already in use.");
        }

        // Create User
        var user = new ApplicationUser
        {
            UserName = registerDoctorDTO.Email,
            Email = registerDoctorDTO.Email
        };

        var result = await _userManager.CreateAsync(user, registerDoctorDTO.Password);
        if (!result.Succeeded)
        {
            return BadRequest("User registration failed.");
        }
        var roleResult = await _userManager.AddToRoleAsync(user, "Doctor");
        if (!roleResult.Succeeded)
        {
            return BadRequest("Failed to assign role.");
        }

        // Add User to DoctorDetails table
        var doctorDetails = new DoctorDetails
        {
            UserId = user.Id,
            FullName = registerDoctorDTO.FullName,
            PhoneNumber = registerDoctorDTO.PhoneNumber,
            Gender = registerDoctorDTO.Gender,
            DateOfBirth = registerDoctorDTO.DateOfBirth,
            LicenseNumber = registerDoctorDTO.LicenseNumber,
            MedicalCollege = registerDoctorDTO.MedicalCollege,
            Specialization = registerDoctorDTO.Specialization,
            YearsOfExperience = registerDoctorDTO.YearsOfExperience,
            ClinicName = registerDoctorDTO.ClinicName,
            ClinicAddress = registerDoctorDTO.ClinicAddress,
            ConsultationFee = registerDoctorDTO.ConsultationFee,
            CreatedAt = DateTime.UtcNow
        };

        _context.DoctorDetails.Add(doctorDetails);
        await _context.SaveChangesAsync();

        return Ok("Doctor registered successfully.");
    }

    


    // [HttpPost]
    // [AllowAnonymous]
    // public async Task<IActionResult> CompleteDoctorDetails(DoctorRegistrationViewModel model)
    // {
    //     try
    //     {
    //         if (!ModelState.IsValid)
    //             return ValidationError();
    //         
    //         //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    //         var userId = _userManager.GetUserId(User);
    //         
    //         var doctorDetails = new DoctorDetails
    //         {
    //             FullName = model.FullName,
    //             UserId = userId, // Assuming you get UserId from the model
    //             PhoneNumber = model.PhoneNumber, // Assuming you get PhoneNumber from the model
    //             Gender = model.Gender,
    //             DateOfBirth = model.DateOfBirth,
    //             LicenseNumber = model.LicenseNumber,
    //             MedicalCollege = model.MedicalCollege,
    //             Specialization = model.Specialization,
    //             YearsOfExperience = model.YearsOfExperience,
    //             ClinicName = model.ClinicName,
    //             ClinicAddress = model.ClinicAddress,
    //             ConsultationFee = model.ConsultationFee,
    //             ProfileImage = model.ProfileImage,
    //             CreatedAt = DateTime.UtcNow // Automatically set the current time
    //         };
    //         
    //         _context.DoctorDetails.Add(doctorDetails);
    //         await _context.SaveChangesAsync();
    //         return ApiResponse(new { doctorId = doctorDetails.DoctorId }, "Doctor registration successful");
    //     }
    /*
    var user = await _userManager.FindByEmailAsync(model.Email);

    _logger.LogInformation("Doctor details created for user {UserId}.", user.Id);
    // Return the response
    var roles = await _userManager.GetRolesAsync(user);
    return ApiResponse(new
    {
        user = new
        {
            id = user.Id,
            email = user.Email,
            roles,
            emailConfirmed = user.EmailConfirmed
        }
    }, "Doctor registration successful");
    */
    // catch (Exception ex)
    // {
    //     _logger.LogError(ex, "An error occurred while registering the doctor");
    //
    //     // Return a structured error response
    //     return BadRequest(new
    //     {
    //         success = false,
    //         message = "An error occurred while registering your doctor",
    //         errors = ex.Message // You can add more detailed error information here
    //     });
    // }
    //
}