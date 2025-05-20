using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TeleMedicineApp.Controllers;
using TeleMedicineApp.Data;
using TeleMedicineApp.Models;
using TeleMedicineApp.Services;
using TeleMedicineApp.ViewModels;

namespace TeleMedicineApp.Areas.Admin.Controllers;

[Authorize]  // Default authorization for all actions
[Area("Admin")]
[Route("api/[area]/[controller]")]
[ApiController]
public class AccountController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger _logger;
    private readonly IJwtService _jwtService;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;


    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        ILoggerFactory loggerFactory,
        IJwtService jwtService,
        IConfiguration configuration,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _logger = loggerFactory.CreateLogger<AccountController>();
        _jwtService = jwtService;
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("Login")]
    [AllowAnonymous]  // Override the controller-level authorization for login
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationError();

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                var roles = await _userManager.GetRolesAsync(user);
                var (token, refreshToken) = await _jwtService.GenerateTokensAsync(user, roles);

                _logger.LogInformation("User {Email} logged in successfully", model.Email);

                return ApiResponse(new
                {
                    token,
                    refreshToken,
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        roles
                    }
                }, "Login successful");
            }

            if (result.RequiresTwoFactor)
            {
                return ApiResponse(new { requiresTwoFactor = true });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out", model.Email);
                return ApiError("Account locked out", statusCode: 423);
            }

            return UnauthorizedError("Invalid credentials");    
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", model.Email);
            return ApiError("Internal server error", statusCode: 500);
        }
    }

    [HttpPost("RefreshToken")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
    {
        try
        {
            var (newToken, newRefreshToken) = await _jwtService.RefreshTokenAsync(refreshToken);

            return ApiResponse(new
            {
                token = newToken,
                refreshToken = newRefreshToken
            }, "Token refreshed successfully");
        }
        catch (SecurityTokenException ex)
        {
            return UnauthorizedError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return ApiError("Internal server error", statusCode: 500);
        }
    }

    [HttpPost("Register")]
[AllowAnonymous]
public async Task<IActionResult> Register(RegisterViewModel model)
{
    try
    {
        if (!ModelState.IsValid)
            return ValidationError();

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return ApiError("User already registered with this email.", new List<string> { "Duplicate email." });
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} created a new account", model.Email);

            // Assign role
            if (model.Role == "SuperAdmin")
                await _userManager.AddToRoleAsync(user, "SuperAdmin");
            else if (model.Role == "Admin")
                await _userManager.AddToRoleAsync(user, "Admin");
            else if (model.Role == "Doctor")
                await _userManager.AddToRoleAsync(user, "Doctor");
            else if (model.Role == "Patient")
                await _userManager.AddToRoleAsync(user, "Patient");
            else if (model.Role == "Pharmacist")
                await _userManager.AddToRoleAsync(user, "Pharmacist");

            var roles = await _userManager.GetRolesAsync(user);

            // ✅ Send email with credentials
            var subject = "Your TeleMedicine Account Credentials";
            var message = $@"
Hello,

Your account has been successfully created.

Email: {model.Email}
Password: {model.Password}

Please log in and change your password after your first login.

Regards,
TeleMedicine Team
";
            var emailService = new EmailService(_configuration);
            await emailService.SendEmailAsync(model.Email, subject, message);

            return ApiResponse(new
            {
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    roles,
                    emailConfirmed = user.EmailConfirmed
                }
            }, "Registration successful and credentials emailed.");
        }

        var errors = result.Errors.Select(e => e.Description).ToList();
        return ApiError("Registration failed", errors);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during registration for {Email}", model.Email);
        return ApiError("Internal server error", statusCode: 500);
    }
}


    [HttpPost("Logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out");
        return ApiResponse(true, "Logout successful");
    }

    [HttpPost("ConfirmEmail")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            return ValidationError("User ID and confirmation code are required");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFoundError("User not found");

        var result = await _userManager.ConfirmEmailAsync(user, code);
        if (result.Succeeded)
            return ApiResponse(true, "Email confirmed successfully");

        var errors = result.Errors.Select(e => e.Description).ToList();
        return ApiError("Email confirmation failed", errors);
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
    
    [HttpGet("GetDashboardCounts")]
    public async Task<IActionResult> GetDashboardCounts()
    {
        // Get all user-role mappings with roles
        var userRolesWithNames = await (
            from user in _context.Users
            join userRole in _context.UserRoles on user.Id equals userRole.UserId
            join role in _context.Roles on userRole.RoleId equals role.Id
            select new
            {
                UserId = user.Id,
                RoleName = role.Name
            }
        ).ToListAsync();

        var totalUsers = await _context.Users.CountAsync();
        var totalDoctors = userRolesWithNames.Count(ur => ur.RoleName == "Doctor");
        var totalPatients = userRolesWithNames.Count(ur => ur.RoleName == "Patient");
        var totalPharmacists = userRolesWithNames.Count(ur => ur.RoleName == "Pharmacist");
        var totalAppointments = await _context.Appointments.CountAsync();

        return Ok(new
        {
            TotalUsers = totalUsers,
            TotalDoctors = totalDoctors,
            TotalPatients = totalPatients,
            TotalPharmacists = totalPharmacists,
            TotalAppointments = totalAppointments
        });
    }
    
    [HttpGet("appointment-status-counts")]
    public async Task<IActionResult> GetAppointmentStatusCounts()
    {
        var statusCounts = await _context.Appointments
            .GroupBy(a => a.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync();

        return Ok(statusCounts);
    }
    
    [HttpGet("email-exists")]
    [AllowAnonymous]
    public async Task<IActionResult> EmailExists([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Email is required.");
        }

        var user = await _userManager.FindByEmailAsync(email);
        return Ok(user != null);
    }
}