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
using TeleMedicineApp.Services;

namespace TeleMedicineApp.Areas.Doctor.Controllers;

[Authorize(Roles = "SuperAdmin,Doctor,Patient")]
[Area("Doctor")]
[Route("api/[area]/[action]")]
[ApiController]
public class DoctorAppointmentController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger _logger;
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;

    public DoctorAppointmentController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILoggerFactory loggerFactory,
            EmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = loggerFactory.CreateLogger<DoctorAppointmentController>();
        _emailService = emailService;
    }

    [HttpGet("{doctorId}")]
    public async Task<IActionResult> GetTodaysAppointments(int doctorId)
    {
        var today = DateTime.UtcNow.Date;

        var appointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate == today &&
                        a.Status == "Confirmed")
            .OrderBy(a => a.AppointmentDate)
            .Join(_context.PatientDetails,
                appointment => appointment.PatientId,
                patient => patient.PatientId,
                (appointment, patient) => new { appointment, patient })
            .Join(_context.DoctorDetails,
                appointment => appointment.appointment.DoctorId,
                doctor => doctor.DoctorId,
                (appointment, doctor) => new
                {
                    appointment.appointment.AppointmentId,
                    appointment.appointment.AppointmentDate,
                    appointment.patient.PatientId,
                    PatientName = appointment.patient.FullName,
                    DoctorName = doctor.FullName,
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
        var doctorId = await _context.DoctorDetails
            .Where(d => d.UserId == userId)
            .Select(d => d.DoctorId)
            .FirstOrDefaultAsync();

        if (doctorId == 0)
        {
            return NotFound(new { message = "Doctor not found for the given UserId." });
        }

        return Ok(doctorId);
    }

    [HttpPut("{appointmentId}")]
public async Task<IActionResult> RescheduleAppointment(int appointmentId, [FromBody] RescheduleAppointmentRequest request)
{
    
    if (appointmentId != request.AppointmentId)
    {
        return BadRequest("Appointment ID mismatch.");
    }

    // Fetch the appointment
    var appointment = await _context.Appointments.FindAsync(appointmentId);
    if (appointment == null)
    {
        return NotFound("Appointment not found.");
    }

    int dayOfWeek = (int)request.AppointmentDate.DayOfWeek;

    // Get all availability slots for the doctor on that day
    var availabilitySlots = await _context.DoctorAvailability
        .Where(da => da.DoctorId == appointment.DoctorId && da.DayOfWeek == dayOfWeek)
        .ToListAsync();

    if (!availabilitySlots.Any())
    {
        return BadRequest("Doctor availability not found for the selected appointment day.");
    }

    // Normalize and log times
    TimeSpan requestedStart = TimeSpan.FromMinutes(Math.Floor(request.StartTime.TotalMinutes));
    TimeSpan requestedEnd = TimeSpan.FromMinutes(Math.Ceiling(request.EndTime.TotalMinutes));

    Console.WriteLine($"Requested Time: {requestedStart} - {requestedEnd}");

    foreach (var slot in availabilitySlots)
    {
        Console.WriteLine($"Available Slot: {slot.StartTime} - {slot.EndTime}");
    }

    // Check if requested time falls within any available slot
    var validSlot = availabilitySlots.FirstOrDefault(slot =>
        requestedStart >= slot.StartTime && requestedEnd <= slot.EndTime);

    if (validSlot == null)
    {
        return BadRequest("Appointment time is outside all available time slots.");
    }

    // Apply buffer time
    int bufferMinutes = validSlot.BufferTimeInMinutes;
    TimeSpan bufferedStart = requestedStart - TimeSpan.FromMinutes(bufferMinutes);
    TimeSpan bufferedEnd = requestedEnd + TimeSpan.FromMinutes(bufferMinutes);

    // Check for overlapping appointments
    // Check for overlapping appointments, excluding Cancelled and Completed ones
    bool isOverlapping = await _context.Appointments.AnyAsync(a =>
        a.DoctorId == appointment.DoctorId &&
        a.AppointmentDate == request.AppointmentDate &&
        a.AppointmentId != appointmentId &&
        a.Status != "Cancelled" &&
        a.Status != "Completed" && // ✅ Exclude completed appointments
        (
            (bufferedStart < a.EndTime && bufferedStart >= a.StartTime) ||
            (bufferedEnd > a.StartTime && bufferedEnd <= a.EndTime) ||
            (a.StartTime >= bufferedStart && a.StartTime < bufferedEnd)
        )
    );

    if (isOverlapping)
    {
        return BadRequest("This appointment overlaps with an existing one.");
    }

    // Update appointment
    appointment.AppointmentDate = request.AppointmentDate;
    appointment.StartTime = requestedStart;
    appointment.EndTime = requestedEnd;
    appointment.Status = request.Status ?? "Rescheduled";
    appointment.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();
    try
    {
        var patient = await _context.PatientDetails
            .Where(p => p.PatientId == appointment.PatientId)
            .Select(p => new { p.UserId, p.FullName })
            .FirstOrDefaultAsync();

        var doctor = await _context.DoctorDetails
            .Where(d => d.DoctorId == appointment.DoctorId)
            .Select(d => new { d.UserId, d.FullName })
            .FirstOrDefaultAsync();

        var patientEmail = await _context.Users
            .Where(u => u.Id == patient.UserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();

        var doctorEmail = await _context.Users
            .Where(u => u.Id == doctor.UserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();

        var subject = "Appointment Rescheduled";

        if (!string.IsNullOrWhiteSpace(patientEmail))
        {
            var patientBody = $"Dear {patient.FullName},\n\nYour appointment with Dr. {doctor.FullName} has been rescheduled to {appointment.AppointmentDate:yyyy-MM-dd} from {appointment.StartTime} to {appointment.EndTime}.\n\nThank you,\nTeleMedicine Team";
            await _emailService.SendEmailAsync(patientEmail, subject, patientBody);
        }

        if (!string.IsNullOrWhiteSpace(doctorEmail))
        {
            var doctorBody = $"Dear Dr. {doctor.FullName},\n\nThe appointment with patient {patient.FullName} has been rescheduled to {appointment.AppointmentDate:yyyy-MM-dd} from {appointment.StartTime} to {appointment.EndTime}.\n\nTeleMedicine Team";
            await _emailService.SendEmailAsync(doctorEmail, subject, doctorBody);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send reschedule notification emails.");
        // Continue even if email fails
    }


    return Ok("Appointment rescheduled successfully.");
}

    [HttpPut("{appointmentId}")]
    public async Task<IActionResult> CancelAppointment(int appointmentId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);

        if (appointment == null)
        {
            return NotFound("Appointment not found.");
        }

        appointment.Status = "Cancelled";

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();
        try
        {
            var patient = await _context.PatientDetails
                .Where(p => p.PatientId == appointment.PatientId)
                .Select(p => new { p.UserId, p.FullName })
                .FirstOrDefaultAsync();

            var doctor = await _context.DoctorDetails
                .Where(d => d.DoctorId == appointment.DoctorId)
                .Select(d => new { d.UserId, d.FullName })
                .FirstOrDefaultAsync();

            var patientEmail = await _context.Users
                .Where(u => u.Id == patient.UserId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            var doctorEmail = await _context.Users
                .Where(u => u.Id == doctor.UserId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            var subject = "Appointment Cancelled";

            if (!string.IsNullOrWhiteSpace(patientEmail))
            {
                var patientBody = $"Dear {patient.FullName},\n\nYour appointment with Dr. {doctor.FullName} on {appointment.AppointmentDate:yyyy-MM-dd} at {appointment.StartTime} has been cancelled.\n\nIf this was a mistake, please book a new appointment.\n\nThank you,\nTeleMedicine Team";
                await _emailService.SendEmailAsync(patientEmail, subject, patientBody);
            }

            if (!string.IsNullOrWhiteSpace(doctorEmail))
            {
                var doctorBody = $"Dear Dr. {doctor.FullName},\n\nThe appointment with patient {patient.FullName} on {appointment.AppointmentDate:yyyy-MM-dd} at {appointment.StartTime} has been cancelled.\n\nTeleMedicine Team";
                await _emailService.SendEmailAsync(doctorEmail, subject, doctorBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send cancellation notification emails.");
            // Continue even if email fails
        }

        return Ok("Appointment cancelled successfully.");
    }

    [HttpGet("{doctorId}")]
    public async Task<IActionResult> GetPastAppointments(int doctorId)
    {
        var appointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.Status == "Completed")
            .ToListAsync();

        if (!appointments.Any())
            return NotFound("No completed appointments found for this doctor.");

        var result = new List<object>();

        foreach (var a in appointments)
        {
            var doctorName = await _context.DoctorDetails
                .Where(d => d.DoctorId == a.DoctorId)
                .Select(d => d.FullName)
                .FirstOrDefaultAsync();

            var patientName = await _context.PatientDetails
                .Where(p => p.PatientId == a.PatientId)
                .Select(p => p.FullName)
                .FirstOrDefaultAsync();

            result.Add(new
            {
                a.AppointmentId,
                a.AppointmentDate,
                a.StartTime,        // Included StartTime
                a.EndTime,          // Included EndTime
                a.Status,
                a.Reason,
                a.CreatedAt,
                a.UpdatedAt,
                a.AddedBy,
                DoctorId = a.DoctorId,
                DoctorName = doctorName,
                PatientId = a.PatientId,
                PatientName = patientName
            });
        }

        return Ok(result);
    }

    [HttpGet("{doctorId}")]
    public async Task<IActionResult> GetUpcomingAppointments(int doctorId)
    {
        var upcomingAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate > DateTime.UtcNow.Date &&
                        a.Status != "Cancelled" && a.Status != "Completed")
            .OrderBy(a => a.AppointmentDate)
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
                    joined.appointment.AppointmentDate,
                    joined.appointment.Status,
                    joined.appointment.Reason,
                    joined.appointment.CreatedAt,
                    joined.appointment.UpdatedAt,
                    joined.appointment.AddedBy,
                    DoctorId = doctor.DoctorId,
                    PatientId = joined.patient.PatientId,
                    PatientName = joined.patient.FullName,
                    DoctorName = doctor.FullName,
                    joined.appointment.StartTime,    // Added StartTime
                    joined.appointment.EndTime       // Added EndTime
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
            .OrderBy(a => a.AppointmentDate)
            .Select(a => new
            {
                a.AppointmentId,
                a.DoctorId,
                a.PatientId,
                a.AppointmentDate,
                a.Status,
                a.Reason,
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
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate == today)
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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return NotFound(new { message = "User not found with the given email." });
        }

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
    
    [HttpGet("{appointmentId}")]
    [AllowAnonymous]
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

    [HttpPost]
    public async Task<IActionResult> CreateConsultation([FromBody] ConsultationCreateDTO dto)
    {
        if (dto == null)
        {
            return BadRequest(new { message = "Consultation data is missing" });
        }

        var appointment = await _context.Appointments.FindAsync(dto.AppointmentId);
        if (appointment == null)
        {
            return NotFound(new { message = "Appointment not found" });
        }

        // Check if consultation already exists for this appointment
        var existingConsultation = await _context.Consultations
            .FirstOrDefaultAsync(c => c.AppointmentId == dto.AppointmentId);

        if (existingConsultation != null)
        {
            return Conflict(new { message = "Consultation already created for this appointment." });
        }

        var consultation = new Consultation
        {
            AppointmentId = dto.AppointmentId,
            Notes = dto.Notes,
            Recommendations = dto.Recommendations,
            CreatedAt = DateTime.UtcNow
        };

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
        var prescription = await _context.Prescriptions
            .Include(p => p.PrescriptionItems)
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
            .Include(p => p.PrescriptionItems)
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

    [HttpPost]
    public async Task<IActionResult> CreatePrescription([FromBody] PrescriptionRequestDTO prescriptionRequest)
    {
        if (prescriptionRequest == null || prescriptionRequest.PrescriptionItems == null ||
            !prescriptionRequest.PrescriptionItems.Any())
        {
            return BadRequest(new { message = "Prescription data is missing or empty" });
        }

        var consultation = await _context.Consultations.FindAsync(prescriptionRequest.ConsultationId);
        if (consultation == null)
        {
            return NotFound(new { message = "Consultation not found" });
        }

        // Check if a prescription already exists for the given ConsultationId
        var existingPrescription = await _context.Prescriptions
            .FirstOrDefaultAsync(p => p.ConsultationId == prescriptionRequest.ConsultationId);

        if (existingPrescription != null)
        {
            return Conflict(new { message = "Prescription already created for this consultation." });
        }

        var prescription = new Prescription
        {
            ConsultationId = prescriptionRequest.ConsultationId,
            PrescribedAt = DateTime.UtcNow
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

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

        await _context.SaveChangesAsync();
        try
{
    // Get the appointment ID from the consultation
    var appointmentId = consultation.AppointmentId;

    // Fetch the patient and doctor info from the appointment
    var appointment = await _context.Appointments
        .Where(a => a.AppointmentId == appointmentId)
        .Select(a => new { a.PatientId, a.DoctorId })
        .FirstOrDefaultAsync();

    if (appointment == null)
    {
        _logger.LogWarning("Appointment not found for consultation ID: {ConsultationId}", consultation.ConsultationId);
        return Ok(new { message = "Prescription created, but email notification skipped (appointment not found)." });
    }

    // Fetch patient's user and email
    var patient = await _context.PatientDetails
        .Where(p => p.PatientId == appointment.PatientId)
        .Select(p => new { p.FullName, p.UserId })
        .FirstOrDefaultAsync();

    var doctor = await _context.DoctorDetails
        .Where(d => d.DoctorId == appointment.DoctorId)
        .Select(d => new { d.FullName })
        .FirstOrDefaultAsync();

    if (patient == null || doctor == null)
    {
        _logger.LogWarning("Patient or doctor not found for appointment ID: {AppointmentId}", appointmentId);
        return Ok(new { message = "Prescription created, but email notification skipped (patient or doctor not found)." });
    }

    var patientEmail = await _context.Users
        .Where(u => u.Id == patient.UserId)
        .Select(u => u.Email)
        .FirstOrDefaultAsync();

    if (!string.IsNullOrWhiteSpace(patientEmail))
    {
        var subject = "New Prescription Added";
        var body = $"Dear {patient.FullName},\n\n" +
                   $"A new prescription has been added following your consultation with Dr. {doctor.FullName}.\n\n" +
                   $"Please log in to your account to view the prescription details.\n\n" +
                   "Thank you,\nTeleMedicine Team";

        await _emailService.SendEmailAsync(patientEmail, subject, body);
    }
    else
    {
        _logger.LogWarning("Email not found for patient with UserId: {UserId}", patient.UserId);
    }
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to send prescription notification email.");
    // Do not interrupt the main flow
}

        return Ok(new { message = "Prescription created successfully" });
    }
    [HttpPost]
    public async Task<IActionResult> SetAvailability(DoctorAvailabilityDto availabilityDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var newStart = availabilityDto.StartTime;  // TimeSpan
        var newEnd = availabilityDto.EndTime;      // TimeSpan

        var buffer = TimeSpan.FromMinutes(availabilityDto.BufferTimeInMinutes);

        // Get all existing availability for the doctor on the same day
        var existingAvailabilities = await _context.DoctorAvailability
            .Where(d => d.DoctorId == availabilityDto.DoctorId && d.DayOfWeek == availabilityDto.DayOfWeek)
            .ToListAsync();

        foreach (var existing in existingAvailabilities)
        {
            // Skip overlap check against itself when updating
            if (availabilityDto.AvailabilityId != 0 && availabilityDto.AvailabilityId == existing.AvailabilityId)
                continue;

            var existingStartWithBuffer = existing.StartTime - TimeSpan.FromMinutes(existing.BufferTimeInMinutes);
            var existingEndWithBuffer = existing.EndTime + TimeSpan.FromMinutes(existing.BufferTimeInMinutes);

            bool overlaps = newStart < existingEndWithBuffer && newEnd > existingStartWithBuffer;

            if (overlaps)
                return BadRequest("Doctor already has availability set that overlaps with this time slot including buffer time.");
        }

        if (availabilityDto.AvailabilityId != 0)
        {
            // Update existing availability
            var existingAvailability = existingAvailabilities.FirstOrDefault(d => d.AvailabilityId == availabilityDto.AvailabilityId);
            if (existingAvailability == null)
                return NotFound("Availability record not found.");

            existingAvailability.StartTime = newStart;
            existingAvailability.EndTime = newEnd;
            existingAvailability.AppointmentDurationInMinutes = availabilityDto.AppointmentDurationInMinutes;
            existingAvailability.BufferTimeInMinutes = availabilityDto.BufferTimeInMinutes;
            existingAvailability.UpdatedAt = DateTime.Now;
        }
        else
        {
            // Add new availability
            var newAvailability = new DoctorAvailability
            {
                DoctorId = availabilityDto.DoctorId,
                DayOfWeek = availabilityDto.DayOfWeek,
                StartTime = newStart,
                EndTime = newEnd,
                AppointmentDurationInMinutes = availabilityDto.AppointmentDurationInMinutes,
                BufferTimeInMinutes = availabilityDto.BufferTimeInMinutes,
                CreatedAt = DateTime.Now
            };

            _context.DoctorAvailability.Add(newAvailability);
        }

        await _context.SaveChangesAsync();

        return Ok("Availability saved successfully.");
    }
    
    [HttpDelete("{availabilityId}")]
    public async Task<IActionResult> DeleteAvailability(int availabilityId)
    {
        var availability = await _context.DoctorAvailability
            .FirstOrDefaultAsync(a => a.AvailabilityId == availabilityId);

        if (availability == null)
        {
            return NotFound("Availability not found.");
        }

        _context.DoctorAvailability.Remove(availability);
        await _context.SaveChangesAsync();

        return Ok("Availability deleted successfully.");
    }
    [HttpGet("{doctorId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailability(int doctorId)
    {
        // Fetch all availability records for this doctor
        var availabilities = await _context.DoctorAvailability
            .Where(d => d.DoctorId == doctorId)
            .OrderBy(d => d.DayOfWeek)
            .ToListAsync();

        if (availabilities == null || availabilities.Count == 0)
            return NotFound("No availability found for the specified doctor.");

        // You can map or return the data directly
        // For example, returning the raw data is fine:
        return Ok(availabilities);
    }
    [HttpGet("{availabilityId}")]
    public async Task<IActionResult> GetAvailabilityByAvailabilityId(int availabilityId)
    {
        // Fetch the availability record with the given ID
        var availability = await _context.DoctorAvailability
            .FirstOrDefaultAsync(d => d.AvailabilityId == availabilityId);

        if (availability == null)
            return NotFound("No availability found for the specified ID.");

        return Ok(availability);
    }
    
    [HttpPut("{availabilityId}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateAvailability(int availabilityId, [FromBody] DoctorAvailability updated)
    {
        var existing = await _context.DoctorAvailability.FindAsync(availabilityId);
        if (existing == null) return NotFound("Availability not found.");

        // Overlap + buffer time check
        var newStart = updated.StartTime;
        var newEnd = updated.EndTime;
        var buffer = TimeSpan.FromMinutes(updated.BufferTimeInMinutes);

        var existingAvailabilities = await _context.DoctorAvailability
            .Where(d => d.DoctorId == existing.DoctorId && d.DayOfWeek == updated.DayOfWeek && d.AvailabilityId != availabilityId)
            .ToListAsync();

        foreach (var other in existingAvailabilities)
        {
            var otherStartWithBuffer = other.StartTime - TimeSpan.FromMinutes(other.BufferTimeInMinutes);
            var otherEndWithBuffer = other.EndTime + TimeSpan.FromMinutes(other.BufferTimeInMinutes);

            bool overlaps = newStart < otherEndWithBuffer && newEnd > otherStartWithBuffer;
            if (overlaps)
            {
                return BadRequest("The updated availability overlaps with an existing one, including buffer time.");
            }
        }

        // Proceed with update
        existing.DayOfWeek = updated.DayOfWeek;
        existing.StartTime = updated.StartTime;
        existing.EndTime = updated.EndTime;
        existing.AppointmentDurationInMinutes = updated.AppointmentDurationInMinutes;
        existing.BufferTimeInMinutes = updated.BufferTimeInMinutes;
        existing.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctorsBySpecialization(string specialization)
    {
        if (string.IsNullOrWhiteSpace(specialization))
            return BadRequest("Specialization is required.");

        var doctors = await (from doctor in _context.DoctorDetails
            join user in _context.Users
                on doctor.UserId equals user.Id
            where doctor.Specialization.ToLower().Contains(specialization.ToLower())
            select new
            {
                doctor.DoctorId,
                doctor.FullName,
                user.Email,
                doctor.PhoneNumber,
                doctor.Specialization
            }).ToListAsync();

        if (doctors == null || !doctors.Any())
            return NotFound("No doctors found for the given specialization.");

        return Ok(doctors);
    }
    
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctorAvailabilityById(int doctorId)
    {
        if (doctorId <= 0)
            return BadRequest("Invalid Doctor ID.");

        var availabilities = await _context.DoctorAvailability
            .Where(a => a.DoctorId == doctorId)
            .OrderBy(a => a.DayOfWeek)
            .Select(a => new
            {
                a.DayOfWeek,
                StartTime = a.StartTime.ToString(@"hh\:mm"),
                EndTime = a.EndTime.ToString(@"hh\:mm"),
                a.AppointmentDurationInMinutes,
                a.BufferTimeInMinutes
            })
            .ToListAsync();

        if (availabilities == null || !availabilities.Any())
            return NotFound("No availability found for this doctor.");

        return Ok(availabilities);
    }
    
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> SearchPatients(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Search query is required.");

        query = query.ToLower();

        var patients = await (from p in _context.PatientDetails
            join u in _context.Users
                on p.UserId equals u.Id
            where p.FullName.ToLower().Contains(query)
                  || u.Email.ToLower().Contains(query)
                  || u.PhoneNumber.Contains(query)
            select new
            {
                p.PatientId,
                p.FullName,
                u.Email,
                p.PhoneNumber,
                p.DateOfBirth,
                p.Gender
            }).ToListAsync();

        if (!patients.Any())
            return NotFound("No matching patients found.");

        return Ok(patients);
    }
    
}