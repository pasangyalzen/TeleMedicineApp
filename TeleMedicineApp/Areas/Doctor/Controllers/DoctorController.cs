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
[Consumes("multipart/form-data")]
public async Task<IActionResult> RegisterDoctor([FromForm] RegisterDoctorDTO dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    // ✅ Log incoming data (for debugging, optional)
    _logger.LogInformation("📥 Registering new doctor: {FullName}, Phone: {PhoneNumber}", dto.FullName, dto.PhoneNumber);

    // ✅ Handle profile image upload
    string imagePath = null;
    if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
    {
        var ext = Path.GetExtension(dto.ProfileImage.FileName).ToLower();
        var allowed = new[] { ".jpg", ".jpeg", ".png" };

        if (!allowed.Contains(ext))
            return BadRequest("Only jpg, jpeg, and png formats are allowed.");

        if (dto.ProfileImage.Length > 2 * 1024 * 1024)
            return BadRequest("Max image size is 2MB.");

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "doctors");
        Directory.CreateDirectory(uploadDir);

        var fileName = Guid.NewGuid().ToString() + ext;
        var filePath = Path.Combine(uploadDir, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await dto.ProfileImage.CopyToAsync(stream);

        imagePath = "/uploads/doctors/" + fileName;
    }

    // ✅ Create and store doctor record
    var doctor = new DoctorDetails
    {
        UserId = dto.UserId,
        FullName = dto.FullName,
        PhoneNumber = dto.PhoneNumber,
        Gender = dto.Gender,
        DateOfBirth = dto.DateOfBirth,
        LicenseNumber = dto.LicenseNumber,
        MedicalCollege = dto.MedicalCollege,
        Specialization = dto.Specialization,
        YearsOfExperience = dto.YearsOfExperience,
        ClinicName = dto.ClinicName,
        ClinicAddress = dto.ClinicAddress,
        ConsultationFee = dto.ConsultationFee,
        ProfileImage = imagePath,
        CreatedAt = DateTime.UtcNow
    };

    _context.DoctorDetails.Add(doctor);
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