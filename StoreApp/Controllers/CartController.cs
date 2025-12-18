using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreApp.Data;
using StoreApp.Models;
using StoreApp.Shared;
using System.Security.Claims;
using System.Text.Json;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "customer")]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CartItemRequest>> GetCart()
        {
            var customerId = ResolveCustomerId();
            if (customerId == null) return Unauthorized();

            var cart = _context.CustomerCarts.FirstOrDefault(c => c.CustomerId == customerId.Value);
            if (cart == null || string.IsNullOrWhiteSpace(cart.CartJson))
                return Ok(new List<CartItemRequest>());

            try
            {
                var items = JsonSerializer.Deserialize<List<CartItemRequest>>(cart.CartJson) ?? new List<CartItemRequest>();
                return Ok(items);
            }
            catch
            {
                return Ok(new List<CartItemRequest>());
            }
        }

        [HttpPost("sync")]
        public async Task<ActionResult> SyncCart([FromBody] List<CartItemRequest> items)
        {
            var customerId = ResolveCustomerId();
            if (customerId == null) return Unauthorized();

            try
            {
                var cart = _context.CustomerCarts.FirstOrDefault(c => c.CustomerId == customerId.Value);
                var payload = JsonSerializer.Serialize(items ?? new List<CartItemRequest>());

                if (cart == null)
                {
                    cart = new CustomerCart
                    {
                        CustomerId = customerId.Value,
                        CartJson = payload,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.CustomerCarts.Add(cart);
                }
                else
                {
                    cart.CartJson = payload;
                    cart.UpdatedAt = DateTime.UtcNow;
                    _context.CustomerCarts.Update(cart);
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Cart sync error: {ex.Message}");
                Console.WriteLine($"❌ Inner exception: {ex.InnerException?.Message}");
                return StatusCode(500, new { message = "Lỗi khi lưu giỏ hàng" });
            }
        }

        private int? ResolveCustomerId()
        {
            // Đọc customerId từ JWT token
            var customerIdClaim = User?.FindFirst("customerId")?.Value;
            if (int.TryParse(customerIdClaim, out var customerId))
                return customerId;

            // Fallback: đọc ClaimTypes.NameIdentifier
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var claimId))
                return claimId;

            if (Request.Headers.TryGetValue("X-User-Id", out var header) &&
                int.TryParse(header, out var headerId))
            {
                return headerId;
            }

            return null;
        }
    }
}
