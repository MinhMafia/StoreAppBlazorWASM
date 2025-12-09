using StoreApp.Services.AI.SemanticSearch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SemanticSearchController : ControllerBase
    {
        private readonly ISemanticSearchService _searchService;
        private readonly IProductIndexingService _indexingService;
        private readonly ILogger<SemanticSearchController> _logger;

        public SemanticSearchController(
            ISemanticSearchService searchService,
            IProductIndexingService indexingService,
            ILogger<SemanticSearchController> logger)
        {
            _searchService = searchService;
            _indexingService = indexingService;
            _logger = logger;
        }

        /// <summary>
        /// Semantic search for products using natural language
        /// Example: "thuốc nhức đầu", "đồ chăm sóc da mụn"
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> SearchProducts(
            [FromQuery] string query,
            [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { error = "Query is required" });

            var results = await _searchService.SearchWithScoresAsync(query, limit);
            return Ok(new
            {
                query,
                count = results.Count,
                results
            });
        }

        /// <summary>
        /// Get current Qdrant index statistics
        /// </summary>
        [HttpGet("stats")]
        [Authorize]
        public async Task<IActionResult> GetStats()
        {
            var indexedCount = await _searchService.GetIndexedCountAsync();
            return Ok(new
            {
                indexedProducts = indexedCount,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Reindex all products to Qdrant (Admin only)
        /// </summary>
        [HttpPost("reindex")]
        [Authorize]
        public async Task<IActionResult> ReindexAllProducts(CancellationToken ct)
        {
            _logger.LogInformation("Starting full product reindex");

            var result = await _indexingService.ReindexAllProductsAsync(ct);

            if (result.Success)
            {
                _logger.LogInformation("Reindex completed: {Indexed} products in {Duration}",
                    result.TotalIndexed, result.Duration);
                return Ok(result);
            }
            else
            {
                _logger.LogError("Reindex failed: {Error}", result.Error);
                return StatusCode(500, result);
            }
        }
    }
}
