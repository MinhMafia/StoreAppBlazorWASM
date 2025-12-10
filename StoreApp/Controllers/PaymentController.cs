using StoreApp.Shared;
using StoreApp.Services;
using Microsoft.AspNetCore.Mvc;
using StoreApp.Models;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentService _paymentService;
        private readonly ActivityLogService _logService;

        public PaymentController(PaymentService paymentService, ActivityLogService logService)
        {
            _paymentService = paymentService;
            _logService = logService;
        }

        [HttpPost("momo/create")]
        public async Task<IActionResult> CreatePayment([FromBody] MomoPaymentRequestDTO req)
        {
            int userId = int.Parse(User.FindFirst("uid")?.Value ?? "0");
            if(userId == 0) return Unauthorized(new { message = "User not authenticated" });

            var result = await _paymentService.CreatePaymentAsync(req, userId);
            if (!result.Success) return BadRequest(new { message = "Tạo payment MoMo thất bại" });
            return Ok(new { payUrl = result.PayUrl, orderId = req.OrderId });
        }

        [HttpPost("momo/ipn")]
        public async Task<IActionResult> MomoCallback([FromBody] MomoIpnCallbackDTO callback)
        {
            bool valid = await _paymentService.HandleMomoCallbackAsync(callback);
            if (!valid)
            {
                await _logService.LogAsync(2, "INVALID_SIGNATURE", "Payment", callback.OrderId ?? "", "Sai chữ ký MoMo", "system");
                return BadRequest(new { message = "Invalid signature" });
            }

            return Ok(new { message = "Callback handled successfully" });
        }

        // POST: api/payment
        [HttpPost("offlinepayment")]
        public async Task<IActionResult> CreateOfflinePayment([FromBody] Payment payment)
        {
            var savedPayment = await _paymentService.CreatePaymentAsync(payment);
            return Ok(savedPayment);
        }

            // Lấy 1 payment duy nhất theo orderId
        [HttpGet("getbyorder/{orderId}")]
        public async Task<IActionResult> GetPayment(int orderId)
        {
            var payment = await _paymentService.GetByOrderIdAsync(orderId);
            if (payment == null)
                return NotFound("Payment not found");

            return Ok(payment);
        }

        [HttpGet("status/{orderId}")]
        public async Task<IActionResult> GetPaymentStatus(int orderId)
        {
            var payment = await _paymentService.GetByOrderIdAsync(orderId);
            if (payment == null)
                return NotFound(new { message = "Payment not found" });

            return Ok(new {
                status = payment.status,
                method = payment.method,
                transaction_ref = payment.transaction_ref
            });
        }
    }
}






