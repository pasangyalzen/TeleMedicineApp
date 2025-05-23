using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeleMedicineApp.Areas.Admin.Models;
using TeleMedicineApp.Areas.Admin.Provider;
using TeleMedicineApp.Areas.Admin.ViewModels;
using TeleMedicineApp.Controllers;
using TeleMedicineApp.Data;
using TeleMedicineApp.Services;

namespace TeleMedicineApp.Areas.Admin.Controllers;
[Authorize(Roles = "SuperAdmin,Doctor,Patient")]
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
    public async Task<IActionResult> GetAllAppointments(int page = 1, int pageSize = 5, string search = "", string sortColumn = "CreatedAt", string sortOrder = "ASC")
    {
        try
        {
            // Calculate the offset based on the current page
            int offset = (page - 1) * pageSize;

            // Call the GetTotalAppointments method with pagination, sorting, and search parameters
            var appointments = await _appointmentManager.GetTotalAppointments(offset, pageSize, search, sortColumn, sortOrder);

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
    ///
    [HttpDelete("{id}")]
    [AllowAnonymous]
    public async Task<object> DeleteAppointment(int appointmentId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);

        if (appointment == null)
            return "Appointment not found";

        // Check if consultations exist
        bool hasConsultations = await _context.Consultations.AnyAsync(c => c.AppointmentId == appointmentId);
        if (hasConsultations)
            return "Cannot delete: has consultations";

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return "Appointment deleted successfully";
    }
    //
    [HttpPut("{appointmentId}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateAppointment(int appointmentId, [FromBody] AppointmentUpdateViewModel updatedAppointment)
    {
        // Validation for appointmentId and updatedAppointment
        if (appointmentId <= 0 || updatedAppointment == null)
        {
            return BadRequest(new { message = "Invalid data. Appointment ID or updated data is missing." });
        }

        // Call the service method to update the appointment in the database
        bool updateSuccess = await _appointmentManager.UpdateAppointment(appointmentId, updatedAppointment);

        // Return appropriate response based on the success of the update operation
        if (updateSuccess)
        {
            return Ok(new { message = "Appointment updated successfully" });
        }
        else
        {
            return BadRequest(new { message = "Failed to update the appointment. No rows were modified." });
        }
    }
    
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> CreateAppointment([FromBody] AppointmentDetailsViewModel model)
    {
        try
        {
            if (model == null)
                return BadRequest("Invalid appointment details");

            // Validate the model
            if (!model.IsValid())
                return BadRequest("Invalid appointment details");

            // Normalize ScheduledTime to remove seconds and milliseconds
            model.NormalizeAppointmentTime();

            string username = User.Identity?.Name ?? "StaticUser";

            // Call backend logic to create appointment
            var result = await _appointmentManager.CreateAppointment(model, username);  

            // Check if the result is successful and send the appropriate success message
            if (result.IsSuccess)
            {
                return Ok(new { message = result.ResultMessage ?? "Appointment created successfully." });
            }

            // If the operation failed, return the error message
            return BadRequest(new { message = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating the appointment");
            return StatusCode(500, new { message = "An error occurred while creating the appointment" });
        }
    }
    [HttpPut("{appointmentId}")]
    public async Task<IActionResult> MarkAppointmentCompleted(int appointmentId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null) return NotFound();

        appointment.Status = "Completed";
        await _context.SaveChangesAsync();

        return Ok(appointment);
    }
}
