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

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStoreService>();

                var count = await vectorStore.GetCountAsync(AiConstants.ProductCollectionName, ct);
                
                if (count == 0)
                {
                    var indexingService = scope.ServiceProvider.GetRequiredService<IProductIndexingService>();
                    await indexingService.ReindexAllProductsAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed during semantic indexing");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
