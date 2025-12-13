using StoreApp.Models;
using StoreApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StoreApp.Shared;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // default authenticated; specific roles per action
    public class OrderItemController : ControllerBase
    {
        private readonly OrderItemService _orderItemService;

        public OrderItemController(OrderItemService orderItemService)
        {
            _orderItemService = orderItemService;
        }

        // POST: api/OrderItem/create
        [Authorize(Roles = "admin,staff,customer")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrderItems([FromBody] List<OrderItemReponse> items)
        {
            if (items == null || !items.Any())
                return BadRequest("Empty list");

            try
            {
                bool success = await _orderItemService.SaveOrderItemsAsync(items);
                return Ok(success);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        // GET: api/OrderItem/by-order/{orderId}
        [Authorize(Roles = "admin,staff,customer")]
        [HttpGet("by-order/{orderId}")]
        public async Task<IActionResult> GetItemsByOrder(int orderId)
        {
            var items = await _orderItemService.GetByOrderIdAsync(orderId);
            return Ok(items);
        }

        // DELETE: api/OrderItem/by-order/{orderId}
        [Authorize(Roles = "admin,staff")]
        [HttpDelete("by-order/{orderId}")]
        public async Task<IActionResult> DeleteItemsByOrder(int orderId)
        {
            await _orderItemService.DeleteByOrderIdAsync(orderId);
            return Ok(new { message = "Đã xóa các item của đơn hàng." });
        }

        [Authorize(Roles = "admin,staff,customer")]
        [HttpGet("byorder/{orderId}")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            var data = await _orderItemService.GetItemsByOrderAsync(orderId);
            return Ok(data);
        }
    }
}
