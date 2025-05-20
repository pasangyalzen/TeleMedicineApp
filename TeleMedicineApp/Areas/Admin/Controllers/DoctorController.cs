using System.Data.SqlClient;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeleMedicineApp.Areas.Admin.Provider;
using TeleMedicineApp.Controllers;
using TeleMedicineApp.Data;
using TeleMedicineApp.Services;
using TeleMedicineApp.Areas.Admin.ViewModels;
using TeleMedicineApp.Models;

namespace TeleMedicineApp.Areas.Admin.Controllers;
[Authorize(Roles = "SuperAdmin,Doctor")]  // Default authorization for all actions
[Area("admin")]
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
    private readonly AppointmentManager _appointmentManager;
    public DoctorController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        ILoggerFactory loggerFactory,
        IJwtService jwtService,
        DoctorManager doctorManager,
        AppointmentManager appointmentManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _logger = loggerFactory.CreateLogger<DoctorController>();
        _jwtService = jwtService;
        _doctorManager = doctorManager;
        _appointmentManager = appointmentManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctors(
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

            // Get paginated doctor list
            var doctors = await _doctorManager.GetTotalDoctors(offset, pageSize, search, sortColumn, sortOrder);

            if (doctors == null || !doctors.Any())
            {
                return NotFound("There are no doctors.");
            }

            return Ok(new
            {
                currentPage = page,
                pageSize = pageSize,
                totalDoctors = doctors.Count,  // Optional: consider using a separate total count SP
                doctors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetDoctors");
            return StatusCode(500, "An error occurred while fetching doctor data.");
        }
    }
    
    
    [HttpGet("{id}")]
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
    [HttpDelete("{userId}")]
    [AllowAnonymous]  // Ensures only Admin can delete a doctor
    public async Task<OperationResponse<string>> DeleteDoctor(string userId)
    {
        var response = new OperationResponse<string>();

        try
        {
            // Step 1: Check if the doctor has any appointments
            var hasAppointments = await _appointmentManager.GetAppointmentsByDoctorUserId(userId);

            if (hasAppointments != null && hasAppointments.Any())
            {
                response.AddError("This doctor cannot be deleted because they have scheduled appointments.");
                response.Result = "Doctor deletion failed.";
                return response;
            }

            // Step 2: Proceed with deleting the doctor
            var result = await _doctorManager.DeleteDoctor(userId);  // Your logic to delete the doctor

            if (result.IsSuccess)
            {
                response.Result = "Doctor deleted successfully.";
            }
            else
            {
                response.AddError("Doctor not found or deletion failed.");
                response.Result = "Doctor deletion failed.";
            }
        }
        catch (Exception ex)
        {
            // Add error to response
            response.AddError($"Error: {ex.Message}");
            response.Result = "Doctor deletion failed.";
        }

        return response;
    }
    [AllowAnonymous]
    [HttpPut("{doctorId}")]
    public async Task<IActionResult> UpdateDoctor(int doctorId, [FromBody] DoctorDetailsViewModel model)
    {
        if (doctorId <= 0)
        {
            return BadRequest(new { message = "Invalid Doctor ID." });
        }

        var existingDoctor = await _doctorManager.GetDoctorById(doctorId);
        if (existingDoctor == null)
        {
            return NotFound(new { message = "Doctor not found." });
        }

        // Check if the DateOfBirth is the current date or a future date, and don't update it
        if (model.DateOfBirth.HasValue && model.DateOfBirth.Value.Date >= DateTime.UtcNow.Date)
        {
            model.DateOfBirth = existingDoctor.DateOfBirth;
        }

        // Proceed with the update logic
        var result = await _doctorManager.UpdateDoctor(model, doctorId);

        if (result)
        {
            return Ok(new { message = "Doctor updated successfully." });
        }

        return StatusCode(500, new { message = "Update executed, but no rows were modified. This could be because the values were already the same." });
    }
    [HttpGet("{doctorId}")]
    [AllowAnonymous]
    public async Task<ActionResult<OperationResponse<string>>> GetUserIdByDoctorId(int doctorId)
    {
        var response = await _doctorManager.GetUserIdByDoctorId(doctorId);

        if (string.IsNullOrEmpty(response.Result))
        {
            return NotFound(response);  // Return 404 if no userId is found
        }

        return Ok(response);  // Return 200 with the response data
    }
    [HttpPut("{doctorId}")] 
    
    public async Task<bool> ToggleDoctorStatus(int doctorId)
    {
        var doctor = await _context.DoctorDetails.FindAsync(doctorId);
        if (doctor == null) return false;

        doctor.IsActive = !doctor.IsActive;  // Toggle status
        doctor.UpdatedAt = DateTime.UtcNow;

        _context.DoctorDetails.Update(doctor);
        await _context.SaveChangesAsync();

        return true;
    }
}
