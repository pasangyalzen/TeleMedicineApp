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
    [Authorize(Roles = "SuperAdmin")]
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
        public async Task<IActionResult> RegisterPharmacist([FromBody] RegisterPharmacistDTO registerPharmacistDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (registerPharmacistDTO.Password != registerPharmacistDTO.ConfirmPassword)
            {
                return BadRequest("Passwords do not match.");
            }

            if (!IsValidPassword(registerPharmacistDTO.Password))
            {
                return BadRequest("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character (@$!%*?&), and must be at least 8 characters long.");
            }

            var existingUser = await _userManager.FindByEmailAsync(registerPharmacistDTO.Email);
            if (existingUser != null)
            {
                return BadRequest("Email is already in use.");
            }

            if (registerPharmacistDTO.DateOfBirth > DateTime.UtcNow.AddYears(-18))
            {
                return BadRequest("Pharmacist must be at least 18 years old.");
            }

            // if (!Uri.IsWellFormedUriString(registerPharmacistDTO.ProfileImage, UriKind.Absolute))
            // {
            //     return BadRequest("Profile image URL is not valid.");
            // }

            var user = new ApplicationUser
            {
                UserName = registerPharmacistDTO.Email,
                Email = registerPharmacistDTO.Email
            };

            var result = await _userManager.CreateAsync(user, registerPharmacistDTO.Password);
            if (!result.Succeeded)
            {
                return BadRequest("User registration failed.");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Pharmacist");
            if (!roleResult.Succeeded)
            {
                return BadRequest("Failed to assign role.");
            }

            var pharmacistDetails = new PharmacistDetails
            {
                UserId = user.Id,
                FullName = registerPharmacistDTO.FullName,
                PhoneNumber = registerPharmacistDTO.PhoneNumber,
                Gender = registerPharmacistDTO.Gender,
                DateOfBirth = registerPharmacistDTO.DateOfBirth,
                LicenseNumber = registerPharmacistDTO.LicenseNumber,
                PharmacyName = registerPharmacistDTO.PharmacyName,
                ProfileImage = registerPharmacistDTO.ProfileImage,
                // Status = true, // Active by default
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
        public async Task<IActionResult> GetAllPharmacists(string search = "", string sortColumn = "CreatedAt", string sortOrder = "ASC", int page = 1, int pageSize = 5)
        {
            try
            {
                var offset = (page - 1) * pageSize;
                var pharmacists = await _pharmacistManager.GetAllPharmacists(offset, pageSize, search, sortColumn, sortOrder);

                if (pharmacists == null || !pharmacists.Any())
                {
                    return NotFound("There are no pharmacists.");
                }

                return Ok(pharmacists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllPharmacists");
                return StatusCode(500, new { message = "Error retrieving pharmacists", details = ex.Message });
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
    }
}