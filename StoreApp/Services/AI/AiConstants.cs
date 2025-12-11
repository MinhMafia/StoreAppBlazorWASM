namespace StoreApp.Services.AI
{
    public static class AiConstants
    {
        #region Token Limits
        public const int MaxToolResultTokens = 8000;

        public const int MaxOutputTokens = 4000;

        public const int ModelContextWindow = 32000;

      
        public const int SafetyBuffer = 500;

      
        public const int MaxSingleMessageTokens = 2000;

        #endregion

        #region Message Limits

       
        public const int MaxMessageLength = 4000;

        public const int MaxHistoryMessages = 40;

        #endregion

        #region Rate Limiting

        public const int RateLimitRequestsPerMinute = 30;

      
        public const int RateLimitCleanupIntervalMinutes = 5;

               public const int RateLimitEntryExpirationMinutes = 10;

        #endregion

        #region Retry & Timeout

        public const int MaxRetryAttempts = 3;

       
        public const int ToolTimeoutSeconds = 30;

        #endregion

        #region Cache

       
        public const int ToolCacheDurationSeconds = 60;

        #endregion

        #region Vector Store

        public const string ProductCollectionName = "products";

        #endregion
    }
}
