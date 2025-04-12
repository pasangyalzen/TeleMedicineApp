using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeleMedicineApp.Areas.Admin.Provider;
using TeleMedicineApp.Controllers;
using TeleMedicineApp.Data;

namespace TeleMedicineApp.Areas.Patients.Controllers;

[Authorize(Roles = "SuperAdmin")] // Default authorization for all actions
[Area("Patient")]
[Route("api/[area]/[action]")]
[ApiController]

public class PatientAppointmentController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger _logger;
    private readonly ApplicationDbContext _context;
    private readonly PatientManager _patientManager;


    public PatientAppointmentController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILoggerFactory loggerFactory,
        PatientManager patientManager)

    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = loggerFactory.CreateLogger<Admin.Controllers.PatientController>();
        _patientManager = patientManager;

    }
    
    [HttpGet("{patientId}")]
    public async Task<IActionResult> GetUpcomingAppointments(int patientId)
    {
        try
        {
            var upcomingAppointments = await _context.Appointments
                .Where(a => a.PatientId == patientId && a.ScheduledTime >= DateTime.UtcNow)
                .Join(_context.DoctorDetails,
                    appointment => appointment.DoctorId,
                    doctor => doctor.DoctorId,
                    (appointment, doctor) => new
                    {
                        appointment.AppointmentId,
                        appointment.ScheduledTime,
                        appointment.Status,
                        appointment.VideoCallLink,
                        DoctorFullName = doctor.FullName
                    })
                .OrderBy(a => a.ScheduledTime)
                .ToListAsync();

            return Ok(upcomingAppointments);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Error retrieving upcoming appointments", details = ex.Message });
        }
    }
    
    [HttpGet("past/{patientId}")]
    public async Task<IActionResult> GetPastAppointments(int patientId)
    {
        try
        {
            var pastAppointments = await _context.Appointments
                .Where(a => a.PatientId == patientId && a.ScheduledTime < DateTime.UtcNow)
                .Join(_context.DoctorDetails,
                    appointment => appointment.DoctorId,
                    doctor => doctor.DoctorId,
                    (appointment, doctor) => new
                    {
                        appointment.AppointmentId,
                        appointment.ScheduledTime,
                        appointment.Status,
                        DoctorFullName = doctor.FullName
                    })
                .OrderByDescending(a => a.ScheduledTime)
                .ToListAsync();

            return Ok(pastAppointments);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Error retrieving past appointments", details = ex.Message });
        }
    }
}