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
    private readonly EmailService _emailService;
    

    public AppointmentController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        ILogger<AppointmentController> logger,
        IJwtService jwtService,
        DoctorManager doctorManager,
        AppointmentManager appointmentManager,
        EmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _logger = logger;
        _jwtService = jwtService;
        _doctorManager = doctorManager;
        _appointmentManager = appointmentManager;
        _emailService = emailService;
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
    
    
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateAppointment([FromBody] AppointmentDetailsViewModel model)
        {
            try
            {
                if (model == null)
                {
                    _logger.LogWarning("CreateAppointment called with null model");
                    return BadRequest("Invalid appointment details: model is null");
                }

                _logger.LogInformation("CreateAppointment called with data: {@Model}", model);

                if (!model.IsValid())
                {
                    _logger.LogWarning("CreateAppointment validation failed: {@Model}", model);
                    return BadRequest("Invalid appointment details: failed validation");
                }

                // Normalize time
                model.NormalizeAppointmentTime();

                _logger.LogInformation("Normalized appointment times: AppointmentDate={AppointmentDate}, StartTime={StartTime}, EndTime={EndTime}",
                    model.AppointmentDate, model.StartTime, model.EndTime);

                string username = User.Identity?.Name ?? "StaticUser";
                _logger.LogInformation("Appointment creation initiated by user: {Username}", username);

                // Fetch user IDs
                var patient = await _context.PatientDetails
                    .Where(p => p.PatientId == model.PatientId)
                    .Select(p => new { p.UserId, p.FullName })
                    .FirstOrDefaultAsync();

                var patientEmail = await _context.Users
                    .Where(u => u.Id == patient.UserId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();

                var doctor = await _context.DoctorDetails
                    .Where(d => d.DoctorId == model.DoctorId)
                    .Select(d => new { d.UserId, d.FullName })
                    .FirstOrDefaultAsync();

                var doctorEmail = await _context.Users
                    .Where(u => u.Id == doctor.UserId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();

                model.PatientName = patient?.FullName ?? model.PatientName;
                model.DoctorName = doctor?.FullName ?? model.DoctorName;

                // Save appointment
                var result = await _appointmentManager.CreateAppointment(model, username);

                if (result == null)
                {
                    _logger.LogError("CreateAppointment returned null result for model: {@Model}", model);
                    return StatusCode(500, "Internal server error while creating appointment");
                } 

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Appointment created successfully: {Message}", result.ResultMessage);

                    try
                    {
                        var subject = "Appointment Confirmation";
                        var body = $"Dear {model.PatientName},\n\nYour appointment with Dr. {model.DoctorName} on {model.AppointmentDate:yyyy-MM-dd} at {model.StartTime} has been successfully scheduled.\n\nThank you,\nTeleMedicine Team";

                        if (!string.IsNullOrWhiteSpace(patientEmail))
                        {
                            await _emailService.SendEmailAsync(patientEmail, subject, body);
                            _logger.LogInformation("Confirmation email sent to patient: {Email}", patientEmail);
                        }

                        if (!string.IsNullOrWhiteSpace(doctorEmail))
                        {
                            var doctorBody = $"Dear Dr. {model.DoctorName},\n\nYou have a new appointment scheduled with {model.PatientName} on {model.AppointmentDate:yyyy-MM-dd} at {model.StartTime}.\n\nTeleMedicine Team";
                            await _emailService.SendEmailAsync(doctorEmail, subject, doctorBody);
                            _logger.LogInformation("Notification email sent to doctor: {Email}", doctorEmail);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send confirmation emails.");
                        // Continue even if email fails
                    }

                    return Ok(new { message = result.ResultMessage ?? "Appointment created successfully." });
                }
                else
                {
                    _logger.LogWarning("CreateAppointment failed: {ErrorMessage}", result.ErrorMessage);
                    return BadRequest(new { message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating appointment with data: {@Model}", model);
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
