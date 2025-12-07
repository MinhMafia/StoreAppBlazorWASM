using StoreApp.Models;
using StoreApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderItemController : ControllerBase
    {
        private readonly OrderItemService _orderItemService;

        public OrderItemController(OrderItemService orderItemService)
        {
            _orderItemService = orderItemService;
        }

        // POST: api/OrderItem/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrderItems([FromBody] List<OrderItem> items)
        {
            if (items == null || !items.Any())
                return BadRequest(false);

            try
            {
                bool success = await _orderItemService.SaveOrderItemsAsync(items);
                return Ok(success);
            }
            catch (Exception)
            {
                return StatusCode(500, false);
            }
        }

        // GET: api/OrderItem/by-order/{orderId}
        [HttpGet("by-order/{orderId}")]
        public async Task<IActionResult> GetItemsByOrder(int orderId)
        {
            var items = await _orderItemService.GetByOrderIdAsync(orderId);
            return Ok(items);
        }

        // DELETE: api/OrderItem/by-order/{orderId}
        [HttpDelete("by-order/{orderId}")]
        public async Task<IActionResult> DeleteItemsByOrder(int orderId)
        {
            await _orderItemService.DeleteByOrderIdAsync(orderId);
            return Ok(new { message = "Đã xóa các item của đơn hàng." });
        }

        [HttpGet("byorder/{orderId}")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            var data = await _orderItemService.GetItemsByOrderAsync(orderId);
            return Ok(data);
        }


    }
}
