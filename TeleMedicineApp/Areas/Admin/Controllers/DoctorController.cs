using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using TeleMedicineApp.Areas.Admin.Provider;
using TeleMedicineApp.Controllers;
using TeleMedicineApp.Data;
using TeleMedicineApp.Services;
using TeleMedicineApp.Areas.Admin.ViewModels;
using TeleMedicineApp.Models;

namespace TeleMedicineApp.Areas.Admin.Controllers;
[Authorize (Roles = "Doctor")]  // Default authorization for all actions
[Area("Admin")]
[Route("api/[area]/[action]")]
[ApiController]

public class DoctorController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger _logger;
    private readonly IJwtService _jwtService;
    private readonly DoctorManager _doctorManager;

    public DoctorController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        ILoggerFactory loggerFactory,
        IJwtService jwtService,
        DoctorManager doctorManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _logger = loggerFactory.CreateLogger<DoctorController>();
        _jwtService = jwtService;
        _doctorManager = doctorManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctors(string search = "", string sortColumn = "CreatedAt", string sortOrder = "ASC", int page = 1, int pageSize = 5)
    {
        try
        {
            var doctors = await _doctorManager.GetTotalDoctors(0, int.MaxValue, "", sortColumn, sortOrder);
            Console.WriteLine(doctors);
            if (doctors == null || !doctors.Any())
            {
                return NotFound("There are no doctors");
            }

            return Ok(doctors);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetDoctors");
            return StatusCode(500, ex.Message);
        }
    }
    
    
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctorById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return NotFound("Invalid Doctor Id");
            }
            var doctor = await _doctorManager.GetDoctorById(id);
            if (doctor == null)
            {
                return NotFound("Doctor not found");
            }
            return Ok(doctor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetDoctorById");
            return StatusCode(500, ex.Message);
        }
    }

    // [HttpPost]
    // [AllowAnonymous]
    // public async Task<IActionResult> UpdateDoctorDetails(DoctorDetailsViewModel model)
    // {
    //     try
    //     {
    //         var username = GetUserName ?? "StaticUser";
    //
    //         // Check for model validation
    //         if (!ModelState.IsValid)
    //             return ValidationError();
    //
    //         // Call the method that completes or updates the doctor details
    //         var result = await _doctorManager.CompleteDoctorDetails(model);
    //
    //         // Check if the update was successful
    //         if (result.IsSucess)
    //         {
    //             return ApiResponse("Doctor details updated successfully");
    //         }
    //
    //         // If something goes wrong, return an error message
    //         return ApiError("Failed to update doctor details");
    //     }
    //     catch (Exception ex)
    //     {
    //         // Log the exception message and return a 500 error
    //         Console.WriteLine(ex.Message);
    //         return ApiError("Internal server error", statusCode: 500);
    //     }
    // }
    [HttpDelete]
    [AllowAnonymous]
    public async Task<IActionResult> DeleteDoctor(string userId)
    {
        var result = await _doctorManager.DeleteDoctor(userId);
        if (result.IsSucess)
        {
            return Ok(result.Result);
        }
        return BadRequest(result.Result);
    }
    
    [AllowAnonymous]
    [HttpPut("{doctorId}")]
    public async Task<IActionResult> UpdateDoctor(int doctorId, [FromBody] DoctorDetailsViewModel model)
    {
        if (doctorId <= 0)
        {
            return BadRequest("Invalid Doctor ID.");
        }

        var existingDoctor = await _doctorManager.GetDoctorById(doctorId);
        if (existingDoctor == null)
        {
            return NotFound("Doctor not found.");
        }

        var result = await _doctorManager.UpdateDoctor(model, doctorId);
        if (result)
        {
            return Ok("Doctor updated successfully.");
        }

        return StatusCode(500, "Failed to update doctor.");
    }

}
