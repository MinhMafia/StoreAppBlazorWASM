using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreApp.Services;
using StoreApp.Shared;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class StatisticsController : ControllerBase
    {
        private readonly StatisticsService _statisticsService;

        public StatisticsController(StatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        // GET api/statistics/overview
        [HttpGet("overview")]
        public async Task<ActionResult<OverviewStatsDTO>> GetOverview()
        {
            try
            {
                var stats = await _statisticsService.GetOverviewStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/statistics/revenue?days=7
        [HttpGet("revenue")]
        public async Task<ActionResult<List<RevenueDataPoint>>> GetRevenue([FromQuery] int days = 7)
        {
            try
            {
                var data = await _statisticsService.GetRevenueByPeriodAsync(days);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/statistics/bestsellers?limit=10&days=7
        [HttpGet("bestsellers")]
        public async Task<ActionResult<List<ProductSalesDTO>>> GetBestSellers(
            [FromQuery] int limit = 10,
            [FromQuery] int days = 7
        )
        {
            try
            {
                var data = await _statisticsService.GetBestSellersAsync(limit, days);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/statistics/lowstock?threshold=10
        [HttpGet("lowstock")]
        public async Task<ActionResult<List<ProductInventoryDTO>>> GetLowStock([FromQuery] int threshold = 10)
        {
            try
            {
                var data = await _statisticsService.GetLowStockProductsAsync(threshold);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/statistics/orders?days=7
        [HttpGet("orders")]
        public async Task<ActionResult<OrderStatsDTO>> GetOrders([FromQuery] int days = 7)
        {
            try
            {
                var stats = await _statisticsService.GetOrderStatsAsync(days);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
