using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreApp.Data;
using StoreApp.Models;
using StoreApp.Shared;
using System.Security.Claims;

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

            var items = _context.CartItems
                .Where(c => c.UserId == userId.Value)
                .Select(c => new CartItemDTO
                {
                    ProductId = c.ProductId,
                    ProductName = c.ProductName,
                    ImageUrl = c.ImageUrl ?? string.Empty,
                    Price = c.Price,
                    Quantity = c.Quantity
                })
                .ToList();

            return Ok(items);
        }

        [HttpPost("sync")]
        public async Task<ActionResult> SyncCart([FromBody] List<CartItemDTO> items)
        {
            var userId = ResolveUserId();
            if (userId == null) return Unauthorized();

            var cartItems = items ?? new List<CartItemDTO>();

            // replace all items for user
            var existing = _context.CartItems.Where(c => c.UserId == userId.Value);
            _context.CartItems.RemoveRange(existing);

            if (cartItems.Any())
            {
                var entities = cartItems.Select(i => new CartItem
                {
                    UserId = userId.Value,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName ?? string.Empty,
                    ImageUrl = i.ImageUrl,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    UpdatedAt = DateTime.UtcNow
                });
                _context.CartItems.AddRange(entities);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        private int? ResolveUserId()
        {
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
