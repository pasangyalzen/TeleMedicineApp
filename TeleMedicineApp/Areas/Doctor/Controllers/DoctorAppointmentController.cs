using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeleMedicineApp.Areas.Doctor.Models;
using TeleMedicineApp.Areas.Doctor.ViewModels;
using TeleMedicineApp.Controllers;
using TeleMedicineApp.Data;

namespace TeleMedicineApp.Areas.Doctor.Controllers;

[Authorize(Roles = "SuperAdmin,Doctor")] // Default authorization for all actions
[Area("Doctor")]
[Route("api/[area]/[action]")]
[ApiController]

public class DoctorAppointmentController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger _logger;
    private readonly ApplicationDbContext _context;


    public DoctorAppointmentController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILoggerFactory loggerFactory)

    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = loggerFactory.CreateLogger<DoctorAppointmentController>();

    }
    [HttpGet("{doctorId}")]
    public async Task<IActionResult> GetTodaysAppointments(int doctorId)
    {
        var today = DateTime.UtcNow.Date; // Get today's date (UTC)

        var appointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.ScheduledTime.Date == today) // Use ScheduledTime
            .OrderBy(a => a.ScheduledTime) // Sort by ScheduledTime
            .Join(_context.PatientDetails, 
                appointment => appointment.PatientId, 
                patient => patient.PatientId, 
                (appointment, patient) => new { appointment, patient }) // Join with PatientDetails
            .Join(_context.DoctorDetails, 
                appointment => appointment.appointment.DoctorId, 
                doctor => doctor.DoctorId, 
                (appointment, doctor) => new 
                {
                    appointment.appointment.AppointmentId,
                    appointment.appointment.ScheduledTime,
                    appointment.patient.PatientId,
                    PatientName = appointment.patient.FullName, // Assuming FullName in PatientDetails
                    DoctorName = doctor.FullName, // Assuming FullName in DoctorDetail
                    appointment.appointment.Status
                })
            .ToListAsync();

        if (!appointments.Any()) return NotFound("No appointments for today.");

        return Ok(appointments);
    }
    [HttpGet("{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctorIdByUserId(string userId)
    {
        // Find the doctor using the userId as a foreign key and only retrieve the DoctorId
        var doctorId = await _context.DoctorDetails
            .Where(d => d.UserId == userId)
            .Select(d => d.DoctorId)
            .FirstOrDefaultAsync();

        // Check if doctorId is found, if not return NotFound
        if (doctorId == 0) // 0 indicates no record was found
        {
            return NotFound(new { message = "Doctor not found for the given UserId." });
        }

        // Return the DoctorId
        return Ok(doctorId);
    }
    [HttpPut("RescheduleAppointment/{appointmentId}")]
    public async Task<IActionResult> RescheduleAppointment(int appointmentId, [FromBody] DateTime newDateTime)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
            return NotFound("Appointment not found.");

        appointment.ScheduledTime = newDateTime;
        appointment.Status = "Rescheduled"; // Update status if needed

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        return Ok("Appointment rescheduled successfully.");
    }
    
}