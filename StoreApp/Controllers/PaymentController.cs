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
            int userId = 1;
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
                await _logService.LogAsync(0, "INVALID_SIGNATURE", "Payment", callback.OrderId ?? "", "Sai chữ ký MoMo", "system");
                return BadRequest(new { message = "Invalid signature" });
            }

            await _logService.LogAsync(0, "MOMO_IPN_RECEIVED", "Payment", callback.OrderId ?? "", $"Callback: {callback.Message}", "system");
            return Ok(new { message = "Callback handled successfully" });
        }

        // POST: api/payment
        [HttpPost("offlinepayment")]
        public async Task<IActionResult> CreateOfflinePayment([FromBody] Payment payment)
        {
            var savedPayment = await _paymentService.CreatePaymentAsync(payment);
            return Ok(savedPayment);
        }
    }
}






