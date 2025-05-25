using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TeleMedicineApp.Areas.Pharmacist.Models;
using TeleMedicineApp.Areas.Pharmacist.ViewModels;
using TeleMedicineApp.Data;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TeleMedicineApp.Areas.Admin.Models;
using TeleMedicineApp.Areas.Admin.Provider;
using TeleMedicineApp.Areas.Admin.ViewModels;
using TeleMedicineApp.Controllers;
using TeleMedicineApp.Areas.Pharmacist.ViewModels;

namespace TeleMedicineApp.Areas.Pharmacist.Controllers
{
    [Authorize(Roles = "SuperAdmin, Pharmacist")]
    [Area("Pharmacist")]
    [Route("api/[area]/[action]")]
    [ApiController]
    public class PharmacistController : ApiControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _context;
        private readonly PharmacistManager _pharmacistManager;

        public PharmacistController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILoggerFactory loggerFactory,
            PharmacistManager pharmacistManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = loggerFactory.CreateLogger<PharmacistController>();
            _pharmacistManager = pharmacistManager;
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
            public async Task<IActionResult> RegisterPharmacist([FromForm] RegisterPharmacistDTO dto)
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _logger.LogInformation("📥 Registering new pharmacist: {FullName}, Phone: {PhoneNumber}", dto.FullName, dto.PhoneNumber);

                // Handle profile image upload
                string imagePath = null;
                if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
                {
                    var ext = Path.GetExtension(dto.ProfileImage.FileName).ToLower();
                    var allowed = new[] { ".jpg", ".jpeg", ".png" };

                    if (!allowed.Contains(ext))
                        return BadRequest("Only jpg, jpeg, and png formats are allowed.");

                    if (dto.ProfileImage.Length > 2 * 1024 * 1024)
                        return BadRequest("Max image size is 2MB.");

                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pharmacists");
                    Directory.CreateDirectory(uploadDir);

                    var fileName = Guid.NewGuid().ToString() + ext;
                    var filePath = Path.Combine(uploadDir, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await dto.ProfileImage.CopyToAsync(stream);

                    imagePath = "/uploads/pharmacists/" + fileName;
                }

                // Create and store pharmacist record
                var pharmacist = new PharmacistDetails
                {
                    UserId = dto.UserId,
                    FullName = dto.FullName,
                    PhoneNumber = dto.PhoneNumber,
                    Gender = dto.Gender,
                    DateOfBirth = dto.DateOfBirth,
                    PharmacyName = dto.PharmacyName,
                    LicenseNumber = dto.LicenseNumber,
                    PharmacyAddress = dto.PharmacyAddress,
                    WorkingHours = dto.WorkingHours,
                    ServicesOffered = dto.ServicesOffered,
                    ProfileImage = imagePath,
                    CreatedAt = DateTime.UtcNow,
                    DoctorId = dto.DoctorId,
                    PatientId = dto.PatientId,
                    Status = true // Set default status as active (or handle as needed)
                };

                _context.PharmacistDetails.Add(pharmacist);
                await _context.SaveChangesAsync();

                return Ok("Pharmacist registered successfully.");
            }

        private bool IsValidPassword(string password)
        {
            var pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";
            return Regex.IsMatch(password, pattern);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllPharmacists(
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

                // Fetch paginated list of pharmacists
                var pharmacists = await _pharmacistManager.GetTotalPharmacists(offset, pageSize, search, sortColumn, sortOrder);

                if (pharmacists == null || !pharmacists.Any())
                {
                    return NotFound("There are no pharmacists.");
                }

                return Ok(new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    pharmacists
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllPharmacists");
                return StatusCode(500, "An error occurred while fetching pharmacist data.");
            }
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<PharmacistDetailsViewModel>> GetPharmacistById(int id)
        {
            var pharmacist = await _pharmacistManager.GetPharmacistById(id);
            if (pharmacist == null)
            {
                return NotFound(new { Message = "Pharmacist not found" });
            }
            return Ok(pharmacist);
        }
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeletePharmacist(string userId)
        {
            try
            {
                // Call the manager to handle deletion logic
                var response = await _pharmacistManager.DeletePharmacistByUserId(userId);

                // Check if the deletion was successful
                if (response.IsSuccess)
                {
                    // Return success response with the result message
                    return Ok(new { message = response.Result });
                }
                else
                {
                    // Return error message if deletion was not successful
                    return BadRequest(new { message = response.Result, error = response.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                // Catch any unexpected errors and return a general error message
                return BadRequest(new { message = "Error deleting pharmacist", details = ex.Message });
            }
        }

        [HttpPut("{pharmacistId}")]
        public async Task<IActionResult> UpdatePharmacist(int pharmacistId, [FromBody] PharmacistUpdateViewModel model)
        {
            try
            {
                var pharmacist = await _pharmacistManager.GetPharmacistById(pharmacistId);

                if (pharmacist == null)
                {
                    return BadRequest(new
                    {
                        message = "Error updating pharmacist details",
                        details = "Pharmacist with the given ID does not exist."
                    });
                }

                var isUpdated = await _pharmacistManager.UpdatePharmacistDetails(model, pharmacistId);

                if (isUpdated)
                {
                    return Ok(new { message = "Pharmacist updated successfully." });
                }

                return StatusCode(500,
                    new { message = "Update executed, but no rows were modified. This could be because the" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error updating pharmacist details", details = ex.Message });
            }
        }

        // [HttpGet("{pharmacistId}")]
        // public async Task<IActionResult> GetPharmacistAppointments(int pharmacistId, int offset, int limit)
        // {
        //     try
        //     {
        //         var result = await _pharmacistManager.GetPharmacistAppointments(pharmacistId, offset, limit);
        //         return Ok(result);
        //     }
        //     catch (Exception ex)
        //     {
        //         return BadRequest(new { message = "Error retrieving pharmacist appointments", details = ex.Message });
        //     }
        // }

        [HttpGet("{pharmacistId}")]
        public async Task<IActionResult> GetUserIdByPharmacistId(int pharmacistId)
        {
            try
            {
                var response = await _pharmacistManager.GetUserIdByPharmacistId(pharmacistId);
                if (response.Result.StartsWith("Error"))
                {
                    return BadRequest(response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error retrieving userId for pharmacist", details = ex.Message });
            }
        }
        [HttpPut("{pharmacistId}")]
        public async Task<IActionResult> TogglePharmacistStatus(int pharmacistId)
        {
            var pharmacist = await _context.PharmacistDetails.FindAsync(pharmacistId);
            if (pharmacist == null)
            {
                return NotFound(new { message = "Pharmacist not found." });
            }

            pharmacist.Status = !pharmacist.Status;
            pharmacist.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Pharmacist status updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating status.", error = ex.Message });
            }
        }
        
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCompletedAppointments()
        {
            var appointments = await _context.Appointments
                .Where(a => a.Status == "Completed")
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new
                {
                    a.AppointmentId,
                    a.DoctorId,
                    DoctorName = a.Doctor.FullName,
                    a.PatientId,
                    PatientName = a.Patient.FullName,
                    a.Status,
                    a.CreatedAt,
                    a.UpdatedAt,
                    a.AddedBy,
                    a.AppointmentDate,
                    a.StartTime,
                    a.EndTime,
                    a.AppointmentDurationInMinutes,
                    a.Reason
                })
                .ToListAsync();

            return Ok(appointments);
        }
        
        [HttpGet("{appointmentId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetConsultationPrescriptionsByAppointment(int appointmentId)
        {
            var consultation = await _context.Consultations
                .Include(c => c.Prescriptions)
                .ThenInclude(p => p.PrescriptionItems)
                .Include(c => c.Appointment)
                .ThenInclude(a => a.Doctor)
                .Include(c => c.Appointment)
                .ThenInclude(a => a.Patient)
                .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);

            if (consultation == null)
                return NotFound("No consultation found for the given appointment.");

            var allPrescriptionItems = consultation.Prescriptions?
                .SelectMany(p => p.PrescriptionItems)
                .Select(pi => new PrescriptionItemViewModel
                {
                    MedicineName = pi.MedicineName,
                    Dosage = pi.Dosage,
                    Frequency = pi.Frequency,
                    Duration = pi.Duration,
                    Notes = pi.Notes
                }).ToList() ?? new List<PrescriptionItemViewModel>();

            var result = new ConsultationPrescriptionViewModel
            {
                AppointmentId = consultation.AppointmentId,
                DoctorName = consultation.Appointment.Doctor?.FullName ?? "N/A",
                PatientName = consultation.Appointment.Patient?.FullName ?? "N/A",
                Notes = consultation.Notes,
                Recommendations = consultation.Recommendations,
                PrescriptionItems = allPrescriptionItems
            };

            return Ok(result);
        }
        
        
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllConsultations()
        {
            var consultations = await _context.Consultations
                .Include(c => c.Appointment)
                .ThenInclude(a => a.Doctor)
                .Include(c => c.Appointment)
                .ThenInclude(a => a.Patient)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.ConsultationId,
                    c.AppointmentId,
                    DoctorName = c.Appointment.Doctor.FullName,
                    PatientName = c.Appointment.Patient.FullName,
                    c.Notes,
                    c.Recommendations,
                    c.CreatedAt
                })
                .ToListAsync();

            return Ok(consultations);
        }
        
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllPrescriptions()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .Include(p => p.Consultation)
                .ThenInclude(c => c.Appointment)
                .ThenInclude(a => a.Patient)
                .Include(p => p.Consultation)
                .ThenInclude(c => c.Appointment)
                .ThenInclude(a => a.Doctor)
                .Select(p => new
                {
                    p.PrescriptionId,
                    p.ConsultationId,
                    p.PrescribedAt,
                    DoctorName = p.Consultation.Appointment.Doctor.FullName,
                    PatientName = p.Consultation.Appointment.Patient.FullName,
                    PrescriptionItems = p.PrescriptionItems.Select(pi => new
                    {
                        pi.MedicineName,
                        pi.Dosage,
                        pi.Frequency,
                        pi.Duration,
                        pi.Notes
                    }).ToList()
                })
                .OrderByDescending(p => p.PrescribedAt)
                .ToListAsync();

            return Ok(prescriptions);
        }
        
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetRequestedPrescriptions()
        {
            var prescriptions = await _context.PatientDetails
                .Where(p => p.hasRequested)
                .Join(_context.Appointments.Where(a => a.Status == "Completed"),
                    p => p.PatientId,
                    a => a.PatientId,
                    (p, a) => new { p, a })
                .Join(_context.Consultations,
                    pa => pa.a.AppointmentId,
                    c => c.AppointmentId,
                    (pa, c) => new { pa.p, Consultation = c })
                .Join(_context.Prescriptions,
                    pc => pc.Consultation.ConsultationId,
                    pr => pr.ConsultationId,
                    (pc, pr) => new { pc.p, pc.Consultation, Prescription = pr })
                .SelectMany(
                    pcp => _context.PrescriptionItems
                        .Where(pi => pi.PrescriptionId == pcp.Prescription.PrescriptionId)
                        .Select(pi => new
                        {
                            PatientId = pcp.p.PatientId,
                            PatientName = pcp.p.FullName,
                            PatientPhoto = pcp.p.ProfileImage,    // <-- Added here
                            pcp.Prescription.PrescriptionId,
                            pcp.Prescription.PrescribedAt,
                            pi.MedicineName,
                            pi.Dosage,
                            pi.Frequency,
                            pi.Duration,
                            pi.Notes
                        })
                )
                .ToListAsync();

            return Ok(prescriptions);
        }
        
        
        [HttpGet("{email}")]
        public async Task<IActionResult> GetPharmacistByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email is required.");

            var pharmacist = await _context.PharmacistDetails
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.User.Email.ToLower() == email.ToLower());

            if (pharmacist == null)
                return NotFound("Pharmacist not found.");

            // Return selected fields + email
            return Ok(new
            {
                pharmacist.PharmacistId,
                pharmacist.UserId,
                email = pharmacist.User.Email,
                pharmacist.FullName,
                pharmacist.PhoneNumber,
                pharmacist.Gender,
                pharmacist.DateOfBirth,
                pharmacist.PharmacyName,
                pharmacist.LicenseNumber,
                pharmacist.PharmacyAddress,
                pharmacist.WorkingHours,
                pharmacist.ServicesOffered,
                pharmacist.ProfileImage,
                pharmacist.CreatedAt,
                pharmacist.UpdatedAt,
                pharmacist.DoctorId,
                pharmacist.PatientId
            });
        }
        
        
        
        [HttpGet("{email}")]
        public async Task<IActionResult> GetPharmacistIdByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email is required.");

            var pharmacist = await _context.PharmacistDetails
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.User.Email.ToLower() == email.ToLower());

            if (pharmacist == null)
                return NotFound("Pharmacist not found.");

            return Ok(new { pharmacist.PharmacistId });
        }
    }
}