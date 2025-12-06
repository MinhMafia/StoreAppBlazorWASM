using Microsoft.ML.Tokenizers;
using System.Collections.Concurrent;
using StoreApp.Services.AI;

namespace StoreApp.Services
{
    /// <summary>
    /// Service đếm token chính xác sử dụng Tiktoken (chuẩn OpenAI)
    /// Thay thế phương pháp ước lượng text.Length * 0.7 không chính xác
    ///
    /// Features:
    /// - Sử dụng cl100k_base encoding (GPT-4, GPT-3.5-turbo compatible)
    /// - LRU Cache để tránh đếm lại tokens
    /// - Thread-safe với ConcurrentDictionary
    /// - Sử dụng proper cache key để tránh hash collision
    /// </summary>
    public class TokenizerService
    {
        private readonly Tokenizer _tokenizer;
        private readonly ILogger<TokenizerService> _logger;
        private readonly LruCache<string, int> _tokenCache;

        public TokenizerService(ILogger<TokenizerService> logger)
        {
            _logger = logger;
            _tokenCache = new LruCache<string, int>(AiConstants.TokenCacheMaxSize);

            // Sử dụng cl100k_base encoding (GPT-4, GPT-3.5-turbo)
            _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");

            _logger.LogInformation("TokenizerService initialized with cl100k_base encoding, cache size: {CacheSize}", AiConstants.TokenCacheMaxSize);
        }

        /// <summary>
        /// Đếm số token trong text với caching
        /// Sử dụng text trực tiếp làm key thay vì hashcode để tránh collision
        /// </summary>
        public int CountTokens(string? text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            // Với text dài, dùng hash + length để giảm memory footprint
            // Với text ngắn, dùng text trực tiếp
            var cacheKey = text.Length > 200
                ? $"{text.Length}:{text.GetHashCode()}:{text[..50]}:{text[^50..]}"
                : text;

            if (_tokenCache.TryGet(cacheKey, out var cached))
            {
                return cached;
            }

            var count = _tokenizer.CountTokens(text);
            _tokenCache.Set(cacheKey, count);

            return count;
        }

        /// <summary>
        /// Đếm token cho một message (bao gồm overhead của role)
        /// Theo OpenAI: mỗi message có ~4 tokens overhead
        /// </summary>
        public int CountMessageTokens(string role, string content)
        {
            // Message overhead: <|start|>role<|end|>content<|end|>
            const int MESSAGE_OVERHEAD = 4;
            return CountTokens(content) + MESSAGE_OVERHEAD;
        }

        /// <summary>
        /// Đếm tổng token cho danh sách messages
        /// </summary>
        public int CountMessagesTokens(IEnumerable<(string role, string content)> messages)
        {
            // Base overhead cho conversation: 3 tokens
            const int CONVERSATION_OVERHEAD = 3;

            var total = CONVERSATION_OVERHEAD;
            foreach (var (role, content) in messages)
            {
                total += CountMessageTokens(role, content);
            }
            return total;
        }

        /// <summary>
        /// Ước tính token cho function/tool definitions
        /// Mỗi function khoảng 100-300 tokens tùy độ phức tạp
        /// </summary>
        public int EstimateFunctionTokens(int functionCount)
        {
            // Trung bình ~200 tokens per function definition
            const int TOKENS_PER_FUNCTION = 200;
            return functionCount * TOKENS_PER_FUNCTION;
        }

        /// <summary>
        /// Truncate text để fit trong token limit
        /// Sử dụng binary search để tìm điểm cắt tối ưu
        /// </summary>
        public string TruncateToTokenLimit(string text, int maxTokens)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var tokens = CountTokens(text);
            if (tokens <= maxTokens) return text;

            // Binary search để tìm điểm cắt
            int low = 0;
            int high = text.Length;
            int bestLength = 0;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                var truncated = text[..mid];
                var truncatedTokens = CountTokens(truncated);

                if (truncatedTokens <= maxTokens)
                {
                    bestLength = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            if (bestLength == 0)
            {
                return "[content too long]";
            }

            // Cố gắng cắt ở ranh giới từ
            var result = text[..bestLength];
            var lastSpace = result.LastIndexOf(' ');
            if (lastSpace > bestLength * 0.8) // Chỉ cắt nếu space không quá xa
            {
                result = result[..lastSpace];
            }

            return result + "...[truncated]";
        }

        /// <summary>
        /// Lấy thống kê cache
        /// </summary>
        public (int size, int hits, int misses) GetCacheStats()
        {
            return _tokenCache.GetStats();
        }
    }

    #region LRU Cache Implementation

    /// <summary>
    /// Thread-safe LRU Cache implementation
    /// Tự động evict items cũ nhất khi đầy
    /// </summary>
    public class LruCache<TKey, TValue> where TKey : notnull
    {
        private readonly int _capacity;
        private readonly ConcurrentDictionary<TKey, LinkedListNode<CacheItem>> _cache;
        private readonly LinkedList<CacheItem> _lruList;
        private readonly object _lock = new();
        private int _hits;
        private int _misses;

        public LruCache(int capacity)
        {
            _capacity = capacity;
            _cache = new ConcurrentDictionary<TKey, LinkedListNode<CacheItem>>();
            _lruList = new LinkedList<CacheItem>();
        }

        public bool TryGet(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                lock (_lock)
                {
                    // Move to front (most recently used)
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                }
                Interlocked.Increment(ref _hits);
                value = node.Value.Value;
                return true;
            }

            Interlocked.Increment(ref _misses);
            value = default!;
            return false;
        }

        public void Set(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var existingNode))
                {
                    // Update existing
                    _lruList.Remove(existingNode);
                    existingNode.Value = new CacheItem(key, value);
                    _lruList.AddFirst(existingNode);
                    return;
                }

                // Evict if at capacity
                while (_cache.Count >= _capacity && _lruList.Last != null)
                {
                    var lruNode = _lruList.Last;
                    _lruList.RemoveLast();
                    _cache.TryRemove(lruNode.Value.Key, out _);
                }

                // Add new
                var newNode = new LinkedListNode<CacheItem>(new CacheItem(key, value));
                _lruList.AddFirst(newNode);
                _cache[key] = newNode;
            }
        }

        public (int size, int hits, int misses) GetStats()
        {
            return (_cache.Count, _hits, _misses);
        }

        private class CacheItem
        {
            public TKey Key { get; }
            public TValue Value { get; set; }

            public CacheItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }
    }

    #endregion
}
