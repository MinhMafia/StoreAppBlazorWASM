using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace StoreApp.Services.AI.VectorStore
{
    public class QdrantVectorStoreService : IVectorStoreService
    {
        private readonly QdrantClient _client;
        private readonly ILogger<QdrantVectorStoreService> _logger;
        private readonly HashSet<string> _checkedCollections = new();
        private readonly int _vectorSize;

        public QdrantVectorStoreService(IConfiguration configuration, ILogger<QdrantVectorStoreService> logger)
        {
            _logger = logger;

            var url = configuration["Qdrant:Url"] ?? "";
            var apiKey = configuration["Qdrant:ApiKey"] ?? "";
            _vectorSize = int.TryParse(configuration["Qdrant:VectorSize"], out var size) ? size : 1024;

            var uri = new Uri(url);
            int grpcPort = uri.Port == 6334 ? 6334 : uri.Port;
            
            _client = new QdrantClient(
                host: uri.Host,
                port: grpcPort,
                https: true,
                apiKey: apiKey);
                
            _logger.LogInformation("[Qdrant] Client initialized: {Host}:{Port}", uri.Host, grpcPort);
        }

        public async Task EnsureCollectionAsync(string collectionName, CancellationToken ct = default)
        {
            if (_checkedCollections.Contains(collectionName)) return;

            try
            {
                
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(30));
                
                var exists = await _client.CollectionExistsAsync(collectionName, cts.Token);
                if (!exists)
                {
                    _logger.LogInformation("[Qdrant] Creating collection with vector size {Size}...", _vectorSize);
                    await _client.CreateCollectionAsync(
                        collectionName: collectionName,
                        vectorsConfig: new VectorParams
                        {
                            Size = (ulong)_vectorSize,
                            Distance = Distance.Cosine
                        },
                        cancellationToken: ct);

                    _logger.LogInformation("Created Qdrant collection: {Collection} with vector size: {Size}",
                        collectionName, _vectorSize);
                }

                try
                {
                    await _client.CreatePayloadIndexAsync(
                        collectionName: collectionName,
                        fieldName: "is_active",
                        schemaType: PayloadSchemaType.Bool,
                        cancellationToken: ct);
                    _logger.LogInformation("[Qdrant] Created payload index for 'is_active'");
                }
                catch (Exception ex) when (ex.Message.Contains("already exists"))
                {
                    _logger.LogDebug("[Qdrant] Payload index 'is_active' already exists");
                }

                _checkedCollections.Add(collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Qdrant] Failed to ensure collection exists: {Collection} - {Message}", collectionName, ex.Message);
                throw;
            }
        }

        public async Task UpsertAsync(
            string collectionName,
            int id,
            float[] vector,
            Dictionary<string, object> payload,
            CancellationToken ct = default)
        {
            await EnsureCollectionAsync(collectionName, ct);

            try
            {
                var point = new PointStruct
                {
                    Id = new PointId { Num = (ulong)id },
                    Vectors = vector
                };

                foreach (var kvp in payload)
                {
                    point.Payload[kvp.Key] = ToValue(kvp.Value);
                }

                await _client.UpsertAsync(
                    collectionName: collectionName,
                    points: new[] { point },
                    cancellationToken: ct);

                _logger.LogDebug("Upserted vector: {Id} to collection: {Collection}", id, collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert vector: {Id} to collection: {Collection}", id, collectionName);
                throw;
            }
        }

        public async Task UpsertBatchAsync(
            string collectionName,
            IEnumerable<(int id, float[] vector, Dictionary<string, object> payload)> items,
            CancellationToken ct = default)
        {
            await EnsureCollectionAsync(collectionName, ct);

            var points = items.Select(item =>
            {
                var point = new PointStruct
                {
                    Id = new PointId { Num = (ulong)item.id },
                    Vectors = item.vector
                };

                foreach (var kvp in item.payload)
                {
                    point.Payload[kvp.Key] = ToValue(kvp.Value);
                }

                return point;
            }).ToList();

            if (!points.Any()) return;

            try
            {
                await _client.UpsertAsync(
                    collectionName: collectionName,
                    points: points,
                    cancellationToken: ct);

                _logger.LogInformation("Batch upserted {Count} vectors to collection: {Collection}", points.Count, collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to batch upsert {Count} vectors to collection: {Collection}", points.Count, collectionName);
                throw;
            }
        }

        public async Task DeleteAsync(string collectionName, int id, CancellationToken ct = default)
        {
            try
            {
                await _client.DeleteAsync(
                    collectionName: collectionName,
                    ids: new[] { (ulong)id },
                    cancellationToken: ct);

                _logger.LogDebug("Deleted vector: {Id} from collection: {Collection}", id, collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete vector: {Id} from collection: {Collection}", id, collectionName);
                throw;
            }
        }

        public async Task<IReadOnlyList<(int id, float score, Dictionary<string, object> payload)>> SearchAsync(
            string collectionName,
            float[] queryVector,
            int limit = 10,
            Dictionary<string, object>? filters = null,
            CancellationToken ct = default)
        {
            await EnsureCollectionAsync(collectionName, ct);

            try
            {
                Filter? filter = null;
                if (filters != null && filters.TryGetValue("is_active", out var isActive) && isActive is bool activeValue)
                {
                    filter = new Filter
                    {
                        Must = { new Condition { Field = new FieldCondition
                        {
                            Key = "is_active",
                            Match = new Match { Boolean = activeValue }
                        }}}
                    };
                }

                var searchResult = await _client.SearchAsync(
                    collectionName: collectionName,
                    vector: queryVector,
                    limit: (ulong)limit,
                    filter: filter,
                    payloadSelector: true,
                    cancellationToken: ct);

                return searchResult
                    .Select(r => (
                        id: (int)r.Id.Num,
                        score: r.Score,
                        payload: r.Payload.ToDictionary(
                            p => p.Key,
                            p => FromValue(p.Value))
                    ))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search in collection: {Collection}", collectionName);
                throw;
            }
        }

        public async Task<long> GetCountAsync(string collectionName, CancellationToken ct = default)
        {
            try
            {
                var info = await _client.GetCollectionInfoAsync(collectionName, ct);
                return (long)info.PointsCount;
            }
            catch
            {
                return 0;
            }
        }

        private static Value ToValue(object obj)
        {
            return obj switch
            {
                string s => new Value { StringValue = s },
                int i => new Value { IntegerValue = i },
                long l => new Value { IntegerValue = l },
                double d => new Value { DoubleValue = d },
                float f => new Value { DoubleValue = f },
                bool b => new Value { BoolValue = b },
                null => new Value { NullValue = NullValue.NullValue },
                _ => new Value { StringValue = obj.ToString() ?? "" }
            };
        }

        private static object FromValue(Value value)
        {
            return value.KindCase switch
            {
                Value.KindOneofCase.StringValue => value.StringValue,
                Value.KindOneofCase.IntegerValue => value.IntegerValue,
                Value.KindOneofCase.DoubleValue => value.DoubleValue,
                Value.KindOneofCase.BoolValue => value.BoolValue,
                Value.KindOneofCase.NullValue => null!,
                _ => value.StringValue
            };
        }
    }
}
