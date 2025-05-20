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

            var pharmacistDetails = new PharmacistDetails
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
                // DoctorId = dto.DoctorId,
                // PatientId = dto.PatientId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PharmacistDetails.Add(pharmacistDetails);
            await _context.SaveChangesAsync();

            return Ok("Pharmacist registered successfully.");
        }

        private bool IsValidPassword(string password)
        {
            var pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";
            return Regex.IsMatch(password, pattern);
        }

        [HttpGet]
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
    }
}