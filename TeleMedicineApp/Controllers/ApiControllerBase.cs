using TeleMedicineApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace TeleMedicineApp.Controllers
 {
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        
        protected string? GetUserName
        {
            get
            {
                var emailClaim = User?.Claims.FirstOrDefault(c =>
                    c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
                )?.Value;

                if (!string.IsNullOrEmpty(emailClaim))
                {
                    var username = emailClaim.Split('@')[0];
                    return username;
                }

                return null;
            }

        }
        protected IActionResult ApiResponse<T>(T data, string message = null)
        {
            var response = Models.ApiResponse<T>.SuccessResponse(data, message);
            return Ok(response);
        }

        protected IActionResult ApiError<T>(string message, List<string> errors = null, int statusCode = 400)
        {
            var response = Models.ApiResponse<T>.ErrorResponse(message, errors);
            return StatusCode(statusCode, response);
        }

        protected IActionResult ApiError(string message, List<string> errors = null, int statusCode = 400)
        {
            return ApiError<object>(message, errors, statusCode);
        }

        protected IActionResult ValidationError(string message = "Validation failed")
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return ApiError(message, errors);
        }

        protected IActionResult NotFoundError(string message = "Resource not found")
        {
            return ApiError(message, statusCode: 404);
        }

        protected IActionResult UnauthorizedError(string message = "Unauthorized access")
        {
            return ApiError(message, statusCode: 401);
        }

        protected IActionResult ForbiddenError(string message = "Access forbidden")
        {
            return ApiError(message, statusCode: 403);
        }

        protected IActionResult ServerError(string message = "Internal server error")
        {
            return ApiError(message, statusCode: 500);
        }
    }
}
