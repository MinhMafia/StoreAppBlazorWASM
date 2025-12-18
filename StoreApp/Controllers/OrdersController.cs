using StoreApp.Shared;
using StoreApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // default: must be authenticated; action-level attributes tighten/relax as needed
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;
        public OrdersController(OrderService orderService)
        {
            _orderService = orderService;
        }

        // POS only
        [Authorize(Roles = "admin,staff")]
        [HttpPost("create-temp")]
        public async Task<ActionResult<OrderDTO>> CreateTemporaryOrder()
        {
            try
            {
                var tempOrder = await _orderService.CreateTemporaryOrderAsync();
                return Ok(tempOrder);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Customer (store) + admin/staff
        [Authorize(Roles = "admin,staff,customer")]
        [HttpPost("createonlineordertemp")]
        public async Task<ActionResult<OrderDTO>> CreateTemporaryOnlineOrderAsync()
        {
            try
            {
                var tempOrder = await _orderService.CreateTemporaryOnlineOrderAsync();
                return Ok(tempOrder);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Create order (store checkout & POS)
        [Authorize(Roles = "admin,staff,customer")]
        [HttpPost("create")]
        public async Task<ActionResult<OrderDTO>> CreateOrder([FromBody] OrderDTO dto)
        {
            if (dto == null) return BadRequest(false);

            var created = await _orderService.CreateOrderAsync(dto);

            if (created == null) return BadRequest(false);

            return Ok(created); // trả về OrderDTO đã có Id/OrderNumber
        }

        // Admin dashboard search
        [Authorize(Roles = "admin,staff")]
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            int pageNumber = 1,
            int pageSize = 10,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? search = null
        )
        {
            var result = await _orderService.GetPagedOrdersAsyncForOrderPage(
                pageNumber, pageSize, status, startDate, endDate, search
            );

            return Ok(result);
        }

        /// <summary>
        /// Process order (cash or MoMo)
        /// </summary>
        [Authorize(Roles = "admin,staff")]
        [HttpPost("process")]
        public async Task<IActionResult> ProcessOrder([FromBody] OrderDTO order)
        {
            if (order == null || order.Id <= 0)
                return Ok(false);

            bool success = await _orderService.HandleProcessOrderAsync(order);

            return Ok(success);
        }

        /// <summary>
        /// Cancel pending order (admin/staff)
        /// </summary>
        [Authorize(Roles = "admin,staff")]
        [HttpPost("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                var result = await _orderService.CancelOrderAsync(orderId);
                return Ok(result);
            }
            catch
            {
                return Ok(false);
            }
        }

        // Get order by id (admin/staff) and allow customer to view own order
        [Authorize(Roles = "admin,staff,customer")]
        [HttpGet("getOrderByOrderId/{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var order = await _orderService.GetOrderDtoByIdAsync_MA(orderId);

            if (order == null)
                return NotFound(new { message = $"Order with ID {orderId} not found." });

            var role = User?.FindFirst(ClaimTypes.Role)?.Value;
            if (string.Equals(role, "customer", StringComparison.OrdinalIgnoreCase))
            {
                var customerIdClaim = User?.FindFirst("customerId")?.Value
                    ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(customerIdClaim, out var customerId) || order.CustomerId != customerId)
                {
                    // Allow access for guest-like orders with no customer linked
                    if (order.CustomerId.HasValue)
                    {
                        return Forbid();
                    }
                }
            }

            return Ok(order);
        }

        // Customer: get own order history
        [Authorize(Roles = "customer")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders()
        {
            var customerIdClaim = User?.FindFirst("customerId")?.Value
                ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(customerIdClaim, out var customerId))
            {
                return Forbid();
            }

            var orders = await _orderService.GetOrdersForCustomerAsync(customerId);
            return Ok(orders);
        }

        [Authorize(Roles = "customer")]
        [HttpPut("cancel/{orderId}")]
        public async Task<IActionResult> CancelOrderCustomer(int orderId)
        {
            var result = await _orderService.CancelOrderAsyncCustomer(orderId);

            if (!result)
                return BadRequest("Không thể hủy đơn");

            return Ok(true);
        }
    }
}
