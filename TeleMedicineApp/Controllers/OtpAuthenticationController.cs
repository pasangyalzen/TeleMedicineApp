
using TeleMedicineApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TeleMedicineApp.Services;
using Microsoft.AspNetCore.Authorization;

namespace TeleMedicineApp.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class OtpAuthenticationController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly ILogger<OtpAuthenticationController> _logger;
        public static string _globalVerificationCode; // Global variable for verification code
        public static string _globalEmail; // Global variable for email
        public static DateTime _codeGeneratedTime; // Global variable for code timestamp
        public readonly TimeSpan _codeExpiryTime = TimeSpan.FromMinutes(5);

        public OtpAuthenticationController(EmailService emailService, ILogger<OtpAuthenticationController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("send-code")]
        public async Task<IActionResult> SendVerificationCode([FromBody] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required.");
            }

            email = email.ToLower(); // Normalize email

            // Generate a random 6-digit code
            _globalVerificationCode = new Random().Next(100000, 999999).ToString();
            _globalEmail = email;
            _codeGeneratedTime = DateTime.UtcNow;

            _logger.LogInformation($"Generated code: {_globalVerificationCode} for email: {email}");

            // Send email
            var subject = "Your Email Verification Code";
            var message = $"Your verification code is: {_globalVerificationCode}";
            await _emailService.SendEmailAsync(email, subject, message);

            return Ok("Verification code sent.");
        }

        [HttpPost("verify-code")]
        [AllowAnonymous ]
        public IActionResult VerifyCode([FromBody] VerifyCodeRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Code))
            {
                return BadRequest("Email and code are required.");
            }

            var email = request.Email.ToLower();
            if (_globalEmail != email)
            {
                _logger.LogWarning($"Verification attempt for an email that does not match: {email}");
                return BadRequest("No verification code found for this email.");
            }

            // Check if code has expired
            if (DateTime.UtcNow - _codeGeneratedTime > _codeExpiryTime)
            {
                _logger.LogWarning($"Verification code expired for email: {email}");
                return BadRequest("Verification code has expired.");
            }

            if (_globalVerificationCode == request.Code)
            {
                _logger.LogInformation($"Email {email} successfully verified.");
                _globalVerificationCode = null; // Clear global variables
                _globalEmail = null;
                return Ok("Email verified successfully.");
            }

            _logger.LogWarning($"Invalid verification code for email: {email}. Entered: {request.Code}, Expected: {_globalVerificationCode}");
            return BadRequest("Invalid verification code.");
        }
    }

    public class VerifyCodeRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}
