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

[Authorize(Roles = "Doctor,Patient")] // Default authorization for all actions
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
            .Where(a => a.DoctorId == doctorId && a.ScheduledTime.Date == today &&
                        a.Status != "Cancelled" && a.Status != "Completed") // Use ScheduledTime
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


    [HttpPut("{appointmentId}")]
    public async Task<IActionResult> RescheduleAppointment(int appointmentId, [FromBody] DateTime newDateTime)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
            return NotFound("Appointment not found.");

        // Store the local time directly as UTC in the database
        appointment.ScheduledTime = newDateTime; // This stores the received time as UTC

        appointment.Status = "Rescheduled"; // Update status if needed

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        return Ok("Appointment rescheduled successfully.");
    }

    [HttpPut("{appointmentId}")]
    public async Task<IActionResult> CancelAppointment(int appointmentId)
    {
        // Find the appointment using the appointmentId
        var appointment = await _context.Appointments.FindAsync(appointmentId);

        if (appointment == null)
        {
            return NotFound("Appointment not found.");
        }

        // Update the status to 'Cancelled'
        appointment.Status = "Cancelled";

        // Save the changes to the database
        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        return Ok("Appointment cancelled successfully.");
    }


    [HttpGet("{doctorId}")]
    public async Task<IActionResult> GetPastAppointments(int doctorId)
    {
        // Step 1: Get all completed appointments for the doctor
        var appointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId &&
                        a.ScheduledTime <= DateTime.UtcNow &&
                        a.Status == "Completed")
            .ToListAsync();

        // Step 2: Handle empty result
        if (!appointments.Any())
            return NotFound("No past appointments found for this doctor.");

        // Step 3: Sequentially build the result list
        var result = new List<object>();

        foreach (var a in appointments)
        {
            // Fetch doctor name
            var doctorName = await _context.DoctorDetails
                .Where(d => d.DoctorId == a.DoctorId)
                .Select(d => d.FullName)
                .FirstOrDefaultAsync();

            // Fetch patient name
            var patientName = await _context.PatientDetails
                .Where(p => p.PatientId == a.PatientId)
                .Select(p => p.FullName)
                .FirstOrDefaultAsync();

            // Build the appointment object
            result.Add(new
            {
                a.AppointmentId,
                a.ScheduledTime,
                a.Status,
                a.VideoCallLink,
                a.CreatedAt,
                a.UpdatedAt,
                a.AddedBy,
                DoctorId = a.DoctorId,
                DoctorName = doctorName,
                PatientId = a.PatientId,
                PatientName = patientName
            });
        }

        // Step 4: Return the final result
        return Ok(result);
    }
    
    [HttpGet("{doctorId}")]
    public async Task<IActionResult> GetUpcomingAppointments(int doctorId)
    {
        var upcomingAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.ScheduledTime > DateTime.UtcNow && a.Status != "Cancelled" &&
                        a.Status != "Completed")
            .OrderBy(a => a.ScheduledTime)
            .Join(_context.PatientDetails,
                appointment => appointment.PatientId,
                patient => patient.PatientId,
                (appointment, patient) => new { appointment, patient })
            .Join(_context.DoctorDetails,
                joined => joined.appointment.DoctorId,
                doctor => doctor.DoctorId,
                (joined, doctor) => new
                {
                    joined.appointment.AppointmentId,
                    joined.appointment.ScheduledTime,
                    joined.appointment.Status,
                    joined.appointment.VideoCallLink,
                    joined.appointment.CreatedAt,
                    joined.appointment.UpdatedAt,
                    joined.appointment.AddedBy,
                    DoctorId = doctor.DoctorId, // ✅ explicitly included
                    PatientId = joined.patient.PatientId, // ✅ explicitly included
                    PatientName = joined.patient.FullName,
                    DoctorName = doctor.FullName
                })
            .ToListAsync();

        if (!upcomingAppointments.Any())
            return NotFound("No upcoming appointments.");

        return Ok(upcomingAppointments);
    }



    [HttpPut("{appointmentId}")]
    public async Task<IActionResult> UpdateAppointmentStatus(int appointmentId, [FromBody] string newStatus)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
            return NotFound("Appointment not found.");

        appointment.Status = newStatus;
        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        return Ok("Appointment status updated.");
    }

    [HttpGet("{doctorId}")]
    public async Task<IActionResult> GetCancelledAppointments(int doctorId)
    {
        var cancelledAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.Status == "Cancelled")
            .OrderBy(a => a.ScheduledTime)
            .Select(a => new
            {
                a.AppointmentId,
                a.DoctorId,
                a.PatientId,
                a.ScheduledTime,
                a.Status,
                a.VideoCallLink,
                a.CreatedAt,
                a.UpdatedAt,
                a.AddedBy
            })
            .ToListAsync();

        if (!cancelledAppointments.Any())
            return NotFound("No cancelled appointments.");

        return Ok(cancelledAppointments);
    }

    [HttpGet("{doctorId}")]
    public async Task<IActionResult> GetAppointmentsSummary(int doctorId)
    {
        var today = DateTime.UtcNow.Date;

        var totalAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId)
            .CountAsync();

        var confirmedAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.Status == "Confirmed")
            .CountAsync();

        var completedAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.Status == "Completed")
            .CountAsync();

        var noShowAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.Status == "NoShow")
            .CountAsync();

        var todayAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.ScheduledTime.Date == today)
            .CountAsync();

        var rescheduledAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.Status == "Rescheduled")
            .CountAsync();

        var result = new
        {
            TotalAppointments = totalAppointments,
            ConfirmedAppointments = confirmedAppointments,
            CompletedAppointments = completedAppointments,
            NoShowAppointments = noShowAppointments,
            TodayAppointments = todayAppointments,
            RescheduledAppointments = rescheduledAppointments
        };

        return Ok(result);
    }

    [HttpGet("{email}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctorIdByEmail(string email)
    {
        // Step 1: Find the user by email to get UserId
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return NotFound(new { message = "User not found with the given email." });
        }

        // Step 2: Find the DoctorId using the UserId
        var doctorId = await _context.DoctorDetails
            .Where(d => d.UserId == user.Id)
            .Select(d => d.DoctorId)
            .FirstOrDefaultAsync();

        if (doctorId == 0)
        {
            return NotFound(new { message = "Doctor not found for the given user." });
        }

        return Ok(doctorId);
    }

    [HttpPost]
    public async Task<IActionResult> CreateConsultation([FromBody] ConsultationCreateDTO dto)
    {
        if (dto == null)
        {
            return BadRequest(new { message = "Consultation data is missing" });
        }

        // Validate the AppointmentId
        var appointment = await _context.Appointments.FindAsync(dto.AppointmentId);
        if (appointment == null)
        {
            return NotFound(new { message = "Appointment not found" });
        }

        // Create a new Consultation entity using the data from the DTO
        var consultation = new Consultation
        {
            AppointmentId = dto.AppointmentId,
            Notes = dto.Notes,
            Recommendations = dto.Recommendations,
            CreatedAt = DateTime.UtcNow
        };

        // Add to DB
        _context.Consultations.Add(consultation);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Consultation created successfully",
            consultation
        });
    }

    [HttpGet]
    
    public async Task<IActionResult> GetPrescription(int prescriptionId)
    {
        // Check if the prescription exists
        var prescription = await _context.Prescriptions
            .Include(p => p.PrescriptionItems) // If you want to include related items
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

        if (prescription == null)
        {
            return NotFound(new { message = "Prescription not found" });
        }

        return Ok(prescription);
    }
    
    [HttpGet("{appointmentId}")]
    public async Task<IActionResult> GetPrescriptionByAppointmentId(int appointmentId)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.PrescriptionItems) // Make sure this navigation property exists
            .FirstOrDefaultAsync(p => p.Consultation.AppointmentId == appointmentId);

        if (prescription == null)
            return NotFound();

        var result = new
        {
            prescription.PrescriptionId,
            prescription.ConsultationId,
            prescription.PrescribedAt,
            PrescriptionItems = prescription.PrescriptionItems.Select(item => new
            {
                item.MedicineName,
                item.Dosage,
                item.Frequency,
                item.Duration,
                item.Notes
            }).ToList()
        };

        return Ok(result);
    }

    // 2. Endpoint for creating Prescription
    [HttpPost]
    public async Task<IActionResult> CreatePrescription([FromBody] PrescriptionRequestDTO prescriptionRequest)
    {
        if (prescriptionRequest == null || prescriptionRequest.PrescriptionItems == null ||
            !prescriptionRequest.PrescriptionItems.Any())
        {
            return BadRequest(new { message = "Prescription data is missing or empty" });
        }

        // Validate the ConsultationId
        var consultation = await _context.Consultations.FindAsync(prescriptionRequest.ConsultationId);
        if (consultation == null)
        {
            return NotFound(new { message = "Consultation not found" });
        }

        // Create the Prescription
        var prescription = new Prescription
        {
            ConsultationId = prescriptionRequest.ConsultationId,
            PrescribedAt = DateTime.UtcNow
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync(); // Save the prescription first to get the PrescriptionId

        // Add Prescription Items
        foreach (var item in prescriptionRequest.PrescriptionItems)
        {
            var prescriptionItem = new PrescriptionItem
            {
                PrescriptionId = prescription.PrescriptionId,
                MedicineName = item.MedicineName,
                Dosage = item.Dosage,
                Frequency = item.Frequency,
                Duration = item.Duration,
                Notes = item.Notes
            };

            _context.PrescriptionItems.Add(prescriptionItem);
        }

        // Save the changes for prescription items
        await _context.SaveChangesAsync();

        // Optionally, return the created prescription and items
        return CreatedAtAction(nameof(GetPrescription), new { id = prescription.PrescriptionId }, prescription);
    }

    [HttpGet("{appointmentId}")]
    public async Task<IActionResult> GetConsultationIdByAppointment(int appointmentId)
    {
        // Find the consultation that references the given appointmentId
        var consultation = await _context.Consultations
            .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);

        // If no consultation is found, return NotFound response
        if (consultation == null)
        {
            return NotFound(new { message = "Consultation not found for the given Appointment ID" });
        }

        // Return the ConsultationId in the response
        return Ok(new { consultationId = consultation.ConsultationId });
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