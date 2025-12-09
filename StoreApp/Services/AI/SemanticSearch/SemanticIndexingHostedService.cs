using StoreApp.Services.AI.VectorStore;

namespace StoreApp.Services.AI.SemanticSearch
{
    public class SemanticIndexingHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SemanticIndexingHostedService> _logger;

        public SemanticIndexingHostedService(
            IServiceProvider serviceProvider,
            ILogger<SemanticIndexingHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = Task.Run(() => IndexProductsAsync(cancellationToken), cancellationToken);
            return Task.CompletedTask;
        }

        private async Task IndexProductsAsync(CancellationToken ct)
        {
            await Task.Delay(3000, ct);

            _logger.LogInformation("=== Starting semantic indexing check ===");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStoreService>();

                var count = await vectorStore.GetCountAsync(AiConstants.ProductCollectionName, ct);
                
                if (count == 0)
                {
                    _logger.LogInformation("[Products] Collection '{Collection}' is empty. Starting indexing...", 
                        AiConstants.ProductCollectionName);
                    
                    var indexingService = scope.ServiceProvider.GetRequiredService<IProductIndexingService>();
                    var result = await indexingService.ReindexAllProductsAsync(ct);
                    
                    _logger.LogInformation("[Products] Indexing completed: {Indexed} indexed, Success: {Success}", 
                        result.TotalIndexed, result.Success);
                }
                else
                {
                    _logger.LogInformation("[Products] Collection '{Collection}' already has {Count} items. Skipping.", 
                        AiConstants.ProductCollectionName, count);
                }

                _logger.LogInformation("=== Semantic indexing check completed ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed during semantic indexing: {Message}", ex.Message);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
