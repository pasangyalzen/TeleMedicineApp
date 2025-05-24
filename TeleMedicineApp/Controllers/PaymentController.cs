using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TeleMedicineApp.Services;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace TeleMedicineApp.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Initiates a payment and returns the Khalti payment URL.
        /// </summary>
        [HttpPost("initiate")]
        [AllowAnonymous]
        public async Task<IActionResult> InitiatePayment([FromBody] PaymentRequestModel request)
        {
            if (request == null || request.Amount <= 0 || string.IsNullOrEmpty(request.OrderId))
            {
                return BadRequest(new { message = "Invalid payment request" });
            }

            try
            {
                var paymentUrl = await _paymentService.InitiatePaymentAsync(
                    request.Amount, request.OrderId, request.OrderName,
                    request.CustomerName, request.CustomerEmail, request.CustomerPhone
                );

                if (string.IsNullOrEmpty(paymentUrl))
                {
                    return BadRequest(new { message = "Failed to initiate payment" });
                }

                return Ok(new { paymentUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while initiating payment", error = ex.Message });
            }
        }

        /// <summary>
        /// Verifies a payment when Khalti redirects to this API.
        /// </summary>
        [HttpPost("verify-payment")]  // ✅ Ensure it's POST since your frontend is sending POST
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Pidx) || request.Amount <= 0)
            {
                return BadRequest(new { message = "Invalid payment request" });  // ✅ Improve error message
            }

            bool isVerified = await _paymentService.VerifyPaymentAsync(request.Pidx, request.Amount);

            if (!isVerified)
            {
                return BadRequest(new { message = "Payment verification failed" });
            }

            return Ok(new { message = "Payment verified successfully" });
        }

        // ✅ Create a request model
        public class VerifyPaymentRequest
        {
            public string Pidx { get; set; }
            public decimal Amount { get; set; }
        }

    }


    /// <summary>
    /// Model for payment initiation request.
    /// </summary>
    public class PaymentRequestModel
    {
        public decimal Amount { get; set; }
        public string OrderId { get; set; }
        public string OrderName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
    }

    /// <summary>
    /// Model for payment verification request.
    /// </summary>
    public class PaymentVerificationRequest
    {
        public string pidx { get; set; }

        public decimal amount { get; set; }

    }
}