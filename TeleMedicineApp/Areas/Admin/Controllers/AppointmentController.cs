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
[Route("api/admin/appointments")]
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
    [HttpGet("GetTotalAppointments")]
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
    [HttpGet("GetAppointmentById/{id}")]
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
    // [HttpDelete("DeleteAppointment/{appointmentId}")]
    // [AllowAnonymous]
    // public async Task<IActionResult> DeleteAppointment(int appointmentId)
    // {
    //     try
    //     {
    //         var result = await _appointmentManager.DeleteAppointment(appointmentId);
    //         if (result.IsSuccess)
    //             return Ok(result.Result);
    //         
    //         return BadRequest(result.Result);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error in DeleteAppointment");
    //         return StatusCode(500, ex.Message);
    //     }
    // }
    //
    // /// <summary>
    // /// Updates an existing appointment.
    // /// </summary>
    // [HttpPut("UpdateAppointment/{appointmentId}")]
    // [AllowAnonymous]
    // public async Task<IActionResult> UpdateAppointment(int appointmentId, [FromBody] AppointmentDetailsViewModel model)
    // {
    //     try
    //     {
    //         if (appointmentId <= 0)
    //             return BadRequest("Invalid Appointment Id");
    //         
    //         var existingAppointment = await _appointmentManager.GetAppointmentById(appointmentId);
    //         if (existingAppointment == null)
    //             return NotFound("Appointment not found");
    //         
    //         var result = await _appointmentManager.UpdateAppointment(model, appointmentId);
    //         if (result)
    //             return Ok("Appointment updated successfully");
    //         
    //         return StatusCode(500, "Failed to update appointment");
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error in UpdateAppointment");
    //         return StatusCode(500, ex.Message);
    //     }
    // }
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
