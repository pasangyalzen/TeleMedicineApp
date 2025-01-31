using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeleMedicineApp.Areas.Pharmacist.Models;
using TeleMedicineApp.Areas.Pharmacist.VIewModels;
using TeleMedicineApp.Controllers;
using TeleMedicineApp.Data;
using TeleMedicineApp.Models;


namespace TeleMedicineApp.Areas.Pharmacist.Controllers;

[Authorize(Roles = "Pharmacist")]
[Area("Pharmacist")]
[Route("a   pi/[area]/[action]")]
[ApiController]

public class AccountController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger _logger;
    private readonly ApplicationDbContext _context;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILoggerFactory loggerFactory,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager; 
        _logger = loggerFactory.CreateLogger<AccountController>();
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CompletePharmacistDetails(PharmacistRegistrationViewModel model)
    {
        try
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState);
            var userId = _userManager.GetUserId(User);
            bool doctorExists = await _context.DoctorDetails
                .AnyAsync(d => d.PhoneNumber == model.PhoneNumber ||
                               d.LicenseNumber == model.LicenseNumber);
                           
            if (doctorExists)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "The provided details (Phone Number, License Number) has beem already taken."
                });
            }
            var pharmacistdetails = new PharmacistDetails
            {
                UserId = userId,
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName,
                LicenseNumber = model.LicenseNumber,
                PharmacyName = model.PharmacyName,
                PharmacyAddress = model.PharmacyAddress,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                ProfileImage = model.ProfileImage,
                ServicesOffered = model.ServicesOffered,
                WorkingHours = model.WorkingHours
            };

            _context.Add(pharmacistdetails);
            await _context.SaveChangesAsync();
            return ApiResponse(new { pharmcistid = model.PharmacistId },"Pharmacist Registration Successful");


        }
        catch (Exception)
        {
            return ApiResponse("Failed to complete the registration of the Phamacist");
            
        }
    }
}