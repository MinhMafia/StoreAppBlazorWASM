using StoreApp.Shared;
using StoreApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class ActivityLogController : ControllerBase
    {
        private readonly ActivityLogService _activityLogService;

        public ActivityLogController(ActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }


        /// <summary>
        /// Lấy danh sách log mới nhất có phân trang.
        /// </summary>
        [HttpGet("paged")]
        public async Task<IActionResult> GetPagedLogs([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var (logs, total) = await _activityLogService.GetPagedLogsAsync(page, size);
            return Ok(new
            {
                total,
                page,
                size,
                data = logs
            });
        }

        /// <summary>
        /// Lọc log theo user và thời gian có phân trang.
        /// </summary>
        
        [HttpGet("filter")]
        public async Task<IActionResult> GetFilteredLogs(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] int? userId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var result = await _activityLogService
                    .GetFilteredLogsAsync(page, size, userId, startDate, endDate);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        
        

        // [HttpGet("filter")]
        // public async Task<IActionResult> GetFilteredLogs(
        //     [FromQuery] int page = 1,
        //     [FromQuery] int size = 10,
        //     [FromQuery] int? userId = null,
        //     [FromQuery] DateTime? startDate = null,
        //     [FromQuery] DateTime? endDate = null)
        // {
        //     try
        //     {
        //         var (logs, total) = await _activityLogService.GetFilteredLogsAsync(page, size, userId, startDate, endDate);

               

        //         return Ok(new
        //         {
        //             total,
        //             page,
        //             size,
        //             filters = new { userId, startDate, endDate },
        //             data = logs
        //         });
        //     }
        //     catch (ArgumentException ex)
        //     {
        //         return BadRequest(new { message = ex.Message });
        //     }
        // }


    }
}
