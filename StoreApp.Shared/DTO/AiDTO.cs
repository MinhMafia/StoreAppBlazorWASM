using System.Text.Json.Serialization;

namespace StoreApp.Shared
{
    // ===== REQUEST/RESPONSE DTOs =====

    public class AiChatRequestDTO
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("conversationId")]
        public int? ConversationId { get; set; }

        /// <summary>
        /// Frontend gửi history để không cần load từ DB
        /// </summary>
        [JsonPropertyName("history")]
        public List<ClientMessageDTO>? History { get; set; }
    }

    public class ClientMessageDTO
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response chunk từ SSE stream
    /// </summary>
    public class StreamChunkDTO
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("conversationId")]
        public int? ConversationId { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class AiChatResponseDTO
    {
        public string Response { get; set; } = string.Empty;
        public string? FunctionCalled { get; set; }
        public object? Data { get; set; }
        public bool Success { get; set; } = true;
        public string? Error { get; set; }
        public int? ConversationId { get; set; }
    }

    // ===== CONVERSATION DTOs =====

    /// <summary>
    /// Summary cho list conversations
    /// </summary>
    public class AiConversationSummaryDTO
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Detail với messages
    /// </summary>
    public class AiConversationDTO
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<AiMessageDTO> Messages { get; set; } = new();
    }

    public class AiMessageDTO
    {
        public long Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? FunctionCalled { get; set; }
        public object? Data { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ===== UI CHAT MESSAGE (Client-side) =====

    /// <summary>
    /// Model cho một message trong chat UI
    /// </summary>
    public class ChatMessageDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "user" hoặc "assistant"
        public string Content { get; set; } = string.Empty;
        public bool IsStreaming { get; set; }
        public bool IsError { get; set; }
        public string? FunctionCalled { get; set; }
        public object? Data { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Welcome message
        /// </summary>
        public static ChatMessageDTO CreateWelcomeMessage()
        {
            return new ChatMessageDTO
            {
                Id = "welcome",
                Role = "assistant",
                Content = @"Xin chào! Tôi là trợ lý AI của hệ thống POS. Tôi có thể giúp bạn:

• Xem thống kê doanh thu, đơn hàng
• Tra cứu sản phẩm bán chạy
• Kiểm tra tồn kho
• Tìm kiếm khách hàng, đơn hàng
• Phân tích báo cáo kinh doanh

Bạn cần hỗ trợ gì?"
            };
        }
    }

    // ===== AI TOOL QUERY FILTERS =====

    /// <summary>
    /// Filter cho query_products tool
    /// </summary>
    public class AiProductFilterDTO
    {
        public string? Keyword { get; set; }
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InStock { get; set; }
        public bool? IsActive { get; set; }
        public int Limit { get; set; } = 20;
    }

    /// <summary>
    /// Filter cho query_categories tool
    /// </summary>
    public class AiCategoryFilterDTO
    {
        public string? Keyword { get; set; }
        public bool? IsActive { get; set; }
        public int Limit { get; set; } = 50;
    }

    /// <summary>
    /// Filter cho query_customers tool
    /// </summary>
    public class AiCustomerFilterDTO
    {
        public string? Keyword { get; set; }
        public string? Phone { get; set; }
        public bool? IsActive { get; set; }
        public int Limit { get; set; } = 20;
    }

    /// <summary>
    /// Filter cho query_orders tool
    /// </summary>
    public class AiOrderFilterDTO
    {
        public int? OrderId { get; set; }
        public int? CustomerId { get; set; }
        public string? Status { get; set; } // pending, completed, cancelled
        public string? DateFrom { get; set; } // yyyy-MM-dd
        public string? DateTo { get; set; }
        public string? Keyword { get; set; }
        public int Limit { get; set; } = 20;
    }

    /// <summary>
    /// Filter cho query_promotions tool
    /// </summary>
    public class AiPromotionFilterDTO
    {
        public string? Keyword { get; set; }
        public string? Code { get; set; }
        public bool? IsActive { get; set; }
        public string? Type { get; set; } // percent, fixed
        public int Limit { get; set; } = 20;
    }

    /// <summary>
    /// Filter cho query_suppliers tool
    /// </summary>
    public class AiSupplierFilterDTO
    {
        public string? Keyword { get; set; }
        public bool? IsActive { get; set; }
        public int Limit { get; set; } = 50;
    }

    /// <summary>
    /// Filter cho get_statistics tool
    /// </summary>
    public class AiStatisticsFilterDTO
    {
        public string Type { get; set; } = "overview"; // overview, revenue, best_sellers, low_stock, order_stats
        public int? Days { get; set; } = 7;
        public int? Limit { get; set; } = 10;
        public int? Threshold { get; set; } = 10; // for low_stock
    }

    /// <summary>
    /// Filter cho get_reports tool
    /// </summary>
    public class AiReportFilterDTO
    {
        public string Type { get; set; } = "sales_summary"; // sales_summary, top_products, top_customers, revenue_by_day, sales_by_staff
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
        public int? Limit { get; set; } = 10;
    }
}
