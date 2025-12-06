namespace StoreApp.Shared
{
    public class AiChatRequestDTO
    {
        public string Message { get; set; } = string.Empty;
        public int? ConversationId { get; set; }
        /// <summary>
        /// Frontend gửi history để không cần load từ DB
        /// </summary>
        public List<ClientMessageDTO>? History { get; set; }
    }

    public class ClientMessageDTO
    {
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
    }

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

    public class AiChatResponseDTO
    {
        public string Response { get; set; } = string.Empty;
        public string? FunctionCalled { get; set; }
        public object? Data { get; set; }
        public bool Success { get; set; } = true;
        public string? Error { get; set; }
        public int? ConversationId { get; set; }
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
