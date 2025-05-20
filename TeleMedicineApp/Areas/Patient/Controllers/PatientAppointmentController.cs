using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeleMedicineApp.Areas.Admin.Provider;
using TeleMedicineApp.Controllers;
using TeleMedicineApp.Data;

namespace TeleMedicineApp.Areas.Patients.Controllers;

[Authorize(Roles = "Patient")] // Default authorization for all actions
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
    
    [HttpGet("{appointmentId}")]
    public async Task<IActionResult> GetConsultationByAppointmentId(int appointmentId)
    {
        var consultation = await _context.Consultations
            .Include(c => c.Appointment)
            .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);

        if (consultation == null)
            return NotFound("Consultation not found for the given appointment ID.");

        return Ok(new
        {
            consultation.ConsultationId,
            consultation.AppointmentId,
            consultation.Notes,
            consultation.Recommendations,
            consultation.CreatedAt
        });
    }
    
    [HttpGet("{patientId}")]
    public async Task<IActionResult> GetTodaysAppointmentsByPatient(int patientId)
    {
        var today = DateTime.UtcNow.Date;

        var appointments = await _context.Appointments
            .Where(a => a.PatientId == patientId && a.ScheduledTime.Date == today && a.Status != "Cancelled")
            .OrderBy(a => a.ScheduledTime)
            .Join(_context.DoctorDetails,
                appointment => appointment.DoctorId,
                doctor => doctor.DoctorId,
                (appointment, doctor) => new { appointment, doctor })
            .Join(_context.PatientDetails,
                result => result.appointment.PatientId,
                patient => patient.PatientId,
                (result, patient) => new
                {
                    result.appointment.AppointmentId,
                    result.appointment.ScheduledTime,
                    result.appointment.DoctorId,
                    result.appointment.Status,
                    PatientId = patient.PatientId,
                    PatientName = patient.FullName,
                    DoctorName = result.doctor.FullName
                })
            .ToListAsync();

        if (!appointments.Any())
            return NotFound("No appointments for today.");

        return Ok(appointments);
    }
    [AllowAnonymous]
    [HttpGet("{patientId}")]
    public async Task<IActionResult> GetUpcomingAppointmentsByPatient(int patientId)
    {
        var today = DateTime.UtcNow.Date;

        var upcomingAppointments = await _context.Appointments
            .Where(a => a.PatientId == patientId &&
                        a.ScheduledTime.Date > today && 
                        a.Status != "Cancelled")
            .OrderBy(a => a.ScheduledTime)
            .Join(_context.DoctorDetails,
                appointment => appointment.DoctorId,
                doctor => doctor.DoctorId,
                (appointment, doctor) => new { appointment, doctor })
            .Join(_context.PatientDetails,
                result => result.appointment.PatientId,
                patient => patient.PatientId,
                (result, patient) => new
                {
                    result.appointment.AppointmentId,
                    result.appointment.ScheduledTime,
                    result.appointment.DoctorId,
                    result.appointment.Status,
                    PatientId = patient.PatientId,
                    PatientName = patient.FullName,
                    DoctorName = result.doctor.FullName
                })
            .ToListAsync();

        if (!upcomingAppointments.Any())
            return NotFound("No upcoming appointments found.");

        return Ok(upcomingAppointments);
    }
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetPatientIdByUserId(string userId)
    {
        try
        {
            var patient = await _context.PatientDetails
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
            {
                return NotFound(new { message = "Patient not found for the given user ID." });
            }

            return Ok(new { patient.PatientId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Error retrieving patient ID", details = ex.Message });
        }
    }
    [HttpGet("{patientId}")]
    public async Task<IActionResult> GetPrescriptionsByPatientId(int patientId)
    {
        try
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .Include(p => p.Consultation)
                .ThenInclude(c => c.Appointment)
                .Where(p => p.Consultation.Appointment.PatientId == patientId)
                .Select(p => new
                {
                    p.PrescriptionId,
                    p.ConsultationId,
                    p.PrescribedAt,
                    AppointmentId = p.Consultation.AppointmentId,
                    PrescriptionItems = p.PrescriptionItems.Select(item => new
                    {
                        item.MedicineName,
                        item.Dosage,
                        item.Frequency,
                        item.Duration,
                        item.Notes
                    }).ToList()
                })
                .OrderByDescending(p => p.PrescribedAt)
                .ToListAsync();

            if (!prescriptions.Any())
                return NotFound("No prescriptions found for this patient.");

            return Ok(prescriptions);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Error retrieving prescriptions", details = ex.Message });
        }
    }
    [HttpGet("{appointmentId}")]
    public async Task<IActionResult> GetConsultationIdByAppointmentId(int appointmentId)
    {
        try
        {
            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);

            if (consultation == null)
            {
                return NotFound(new { message = "Consultation not found for the given Appointment ID" });
            }

            return Ok(new { consultationId = consultation.ConsultationId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Error retrieving consultation ID", details = ex.Message });
        }
    }
    
    [HttpGet("{consultationId}")]
    public async Task<IActionResult> GetConsultationById(int consultationId)
    {
        try
        {
            var consultation = await _context.Consultations
                .Where(c => c.ConsultationId == consultationId)
                .Select(c => new
                {
                    c.ConsultationId,
                    c.AppointmentId,
                    c.Notes,
                    c.Recommendations,
                    c.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (consultation == null)
            {
                return NotFound("Consultation not found.");
            }

            return Ok(consultation);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while fetching the consultation: {ex.Message}");
        }
    }
}