namespace StoreApp.Services.AI.VectorStore
{
    public interface IVectorStoreService
    {
        Task EnsureCollectionAsync(string collectionName, CancellationToken ct = default);

        Task UpsertAsync(
            string collectionName,
            int id,
            float[] vector,
            Dictionary<string, object> payload,
            CancellationToken ct = default);

        Task UpsertBatchAsync(
            string collectionName,
            IEnumerable<(int id, float[] vector, Dictionary<string, object> payload)> items,
            CancellationToken ct = default);

        Task DeleteAsync(string collectionName, int id, CancellationToken ct = default);

        Task<IReadOnlyList<(int id, float score, Dictionary<string, object> payload)>> SearchAsync(
            string collectionName,
            float[] queryVector,
            int limit = 10,
            Dictionary<string, object>? filters = null,
            CancellationToken ct = default);

        Task<long> GetCountAsync(string collectionName, CancellationToken ct = default);
    }
}
