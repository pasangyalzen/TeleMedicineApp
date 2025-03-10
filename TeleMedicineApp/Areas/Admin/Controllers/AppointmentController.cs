using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using TeleMedicineApp.Areas.Admin.Provider;
using TeleMedicineApp.Areas.Admin.ViewModels;
using TeleMedicineApp.Controllers;
using TeleMedicineApp.Data;
using TeleMedicineApp.Services;

namespace TeleMedicineApp.Areas.Admin.Controllers;

[ApiController]
[Route("api/admin/appointments/[action]")]
public class AppointmentController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AppointmentController> _logger;
    private readonly IJwtService _jwtService;
    private readonly DoctorManager _doctorManager;
    private readonly AppointmentManager _appointmentManager;

    public AppointmentController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        ILogger<AppointmentController> logger,
        IJwtService jwtService,
        DoctorManager doctorManager,
        AppointmentManager appointmentManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _logger = logger;
        _jwtService = jwtService;
        _doctorManager = doctorManager;
        _appointmentManager = appointmentManager;
    }

    /// <summary>
    /// Retrieves a paginated list of appointments.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetTotalAppointments(int page = 1, int pageSize = 5,string search = "", string sortColumn = "CreatedAt", string sortOrder = "ASC")    {
        try
        {
            var appointments = await _appointmentManager.GetTotalAppointments(0, int.MaxValue, "", sortColumn, sortOrder);
            if (appointments == null || !appointments.Any())
                return NotFound("No appointments found.");
            
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetTotalAppointments");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Retrieves appointment details by ID.
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAppointmentById(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest("Invalid Appointment Id");
            
            var appointment = await _appointmentManager.GetAppointmentById(id);
            if (appointment == null)
                return NotFound("Appointment not found");
            
            return Ok(appointment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAppointmentById");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Deletes an appointment by ID.
    /// </summary>
    [HttpDelete("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var appointmentManager = new AppointmentManager();
        var result = await appointmentManager.DeleteAppointment(id);

        if (result is string && (result.Contains("Cannot delete") || result.Contains("not found")))
        {
            return ApiError(result); // Return the error message from the stored procedure
        }

        return ApiResponse(result); // Success message from the stored procedure
    }
    //
    [HttpPut]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateAppointment(int appointmentId, AppointmentUpdateViewModel updatedAppointment)
{
    // Fetch the existing appointment from the database
    var existingAppointment = await _appointmentManager.GetAppointmentById(appointmentId);
    if (existingAppointment == null)
    {
        return NotFound(new { message = "Appointment not found" });
    }

    bool isUpdated = false; // Track if any field is actually updated
    DateTime? newScheduledTime = null;

    // Handle ScheduledTime update (only if more than 10 minutes difference)
    if (updatedAppointment.ScheduledTime.HasValue && 
        updatedAppointment.ScheduledTime.Value != default(DateTime))
    {
        var timeDifference = updatedAppointment.ScheduledTime.Value - existingAppointment.ScheduledTime;

        if (Math.Abs(timeDifference.TotalMinutes) > 10)
        {
            newScheduledTime = updatedAppointment.ScheduledTime;
            isUpdated = true;
        }
    }

    // Validate Status - Only update if it's different and valid
    var validStatuses = new HashSet<string>
    {
        "Scheduled", "Completed", "Cancelled", "NoShow", "Rescheduled",
        "Pending", "InProgress", "Confirmed", "Rejected", "AwaitingPayment"
    };

    string newStatus = null;
    if (!string.IsNullOrEmpty(updatedAppointment.Status) &&
        updatedAppointment.Status != "string" &&  // Ignore if input is "string"
        updatedAppointment.Status != existingAppointment.Status && // Only update if changed
        validStatuses.Contains(updatedAppointment.Status))
    {
        newStatus = updatedAppointment.Status;
        isUpdated = true;
    }

    // Ensure VideoCallLink is updated only if it's not "string" and it's different from the existing one
    string newVideoCallLink = null;
    if (!string.IsNullOrEmpty(updatedAppointment.VideoCallLink) &&
        updatedAppointment.VideoCallLink != "string" &&  // Ignore if input is "string"
        updatedAppointment.VideoCallLink != existingAppointment.VideoCallLink) // Only update if changed
    {
        newVideoCallLink = updatedAppointment.VideoCallLink;
        isUpdated = true;
    }

    // If no actual changes are detected, return a message
    if (!isUpdated)
    {
        return BadRequest(new { message = "No valid updates provided" });
    }

    // Prepare the data for the update
    var updateData = new AppointmentUpdateViewModel
    {
        ScheduledTime = newScheduledTime,
        Status = newStatus,
        VideoCallLink = newVideoCallLink
    };

    var result = await _appointmentManager.UpdateAppointment(appointmentId, updateData);
    if (result)
    {
        return Ok(new { message = "Appointment updated successfully" });
    }
    else
    {
        return BadRequest(new { message = "Failed to update the appointment" });
    }
}
    //
    // /// <summary>
    // /// Creates a new appointment.
    // /// </summary>
    // [HttpPost("CreateAppointment")]
    // [AllowAnonymous]
    // public async Task<IActionResult> CreateAppointment([FromBody] AppointmentDetailsViewModel model)
    // {
    //     try
    //     {
    //         if (model == null)
    //             return BadRequest("Invalid appointment details");
    //         
    //         var result = await _appointmentManager.CreateAppointment(model);
    //         if (result.IsSuccess)
    //             return Ok("Appointment created successfully");
    //         
    //         return BadRequest(result.Result);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error in CreateAppointment");
    //         return StatusCode(500, ex.Message);
    //     }
    // }
}
