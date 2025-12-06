namespace StoreApp.Services.AI
{
    /// <summary>
    /// Centralized constants cho AI system
    /// Tránh duplicate và magic numbers
    ///
    /// NOTE: Các giá trị có thể override từ appsettings.json qua AiSettings
    /// </summary>
    public static class AiConstants
    {
        #region Token Limits

        /// <summary>
        /// Token tối đa cho tool result (8000 tokens ≈ 32KB text)
        /// </summary>
        public const int MaxToolResultTokens = 8000;

        /// <summary>
        /// Token tối đa cho output của model
        /// </summary>
        public const int MaxOutputTokens = 4000;

        /// <summary>
        /// Context window của model
        /// </summary>
        public const int ModelContextWindow = 32000;

        /// <summary>
        /// Buffer an toàn
        /// </summary>
        public const int SafetyBuffer = 500;

        /// <summary>
        /// Token tối đa cho mỗi tin nhắn đơn lẻ
        /// </summary>
        public const int MaxSingleMessageTokens = 2000;

        #endregion

        #region Message Limits

        /// <summary>
        /// Giới hạn ký tự cho mỗi tin nhắn user (4000 ≈ 1000-1500 tokens)
        /// </summary>
        public const int MaxMessageLength = 4000;

        /// <summary>
        /// Số messages tối đa giữ trong history
        /// </summary>
        public const int MaxHistoryMessages = 40;

        #endregion

        #region Rate Limiting

        /// <summary>
        /// Số request tối đa mỗi phút per user
        /// </summary>
        public const int RateLimitRequestsPerMinute = 30;

        /// <summary>
        /// Thời gian cleanup rate limit data (phút)
        /// </summary>
        public const int RateLimitCleanupIntervalMinutes = 5;

        /// <summary>
        /// Thời gian hết hạn của rate limit entry khi user inactive (phút)
        /// </summary>
        public const int RateLimitEntryExpirationMinutes = 10;

        #endregion

        #region Retry & Timeout

        /// <summary>
        /// Số lần retry khi gọi API thất bại
        /// </summary>
        public const int MaxRetryAttempts = 3;

        /// <summary>
        /// Timeout cho mỗi tool execution (giây)
        /// </summary>
        public const int ToolTimeoutSeconds = 30;

        #endregion

        #region Cache

        /// <summary>
        /// Thời gian cache tool results (giây)
        /// </summary>
        public const int ToolCacheDurationSeconds = 60;

        /// <summary>
        /// Kích thước tối đa của token cache
        /// </summary>
        public const int TokenCacheMaxSize = 2000;

        #endregion

        #region Function Tools

        /// <summary>
        /// Số function tools (ước tính ~200 tokens/tool)
        /// </summary>
        public const int EstimatedFunctionCount = 9;

        #endregion
    }
}
