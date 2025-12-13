using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public ActionResult<IEnumerable<CartItemDTO>> GetCart()
        {
            var userId = ResolveUserId();
            if (userId == null) return Unauthorized();

            var cart = _context.UserCarts.FirstOrDefault(c => c.UserId == userId.Value);
            if (cart == null || string.IsNullOrWhiteSpace(cart.CartJson))
                return Ok(new List<CartItemDTO>());

            try
            {
                var items = JsonSerializer.Deserialize<List<CartItemDTO>>(cart.CartJson) ?? new List<CartItemDTO>();
                return Ok(items);
            }
            catch
            {
                return Ok(new List<CartItemDTO>());
            }
        }

        [HttpPost("sync")]
        public async Task<ActionResult> SyncCart([FromBody] List<CartItemDTO> items)
        {
            var userId = ResolveUserId();
            if (userId == null) return Unauthorized();

            var cart = _context.UserCarts.FirstOrDefault(c => c.UserId == userId.Value);
            var payload = JsonSerializer.Serialize(items ?? new List<CartItemDTO>());

            if (cart == null)
            {
                cart = new UserCart
                {
                    UserId = userId.Value,
                    CartJson = payload,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserCarts.Add(cart);
            }
            else
            {
                cart.CartJson = payload;
                cart.UpdatedAt = DateTime.UtcNow;
                _context.UserCarts.Update(cart);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        private int? ResolveUserId()
        {
            // Token uses "uid" claim; fallback to NameIdentifier for compatibility.
            var idClaim = User?.FindFirst("uid")?.Value ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
