namespace StoreApp.Client.Models.AI
{
    /// <summary>
    /// Model cho một message trong chat
    /// </summary>
    public class ChatMessage
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
        public static ChatMessage CreateWelcomeMessage()
        {
            return new ChatMessage
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
}
