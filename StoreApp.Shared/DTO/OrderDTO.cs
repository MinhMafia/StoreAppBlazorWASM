namespace StoreApp.Shared
{
    public class OrderDTO
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty; // mã đơn hàng "DH+id+timestamp" => Tách cái này ra tim kiếm nha Cường
        public int? CustomerId { get; set; } = 0;
        public int? UserId { get; set; }
        public string Status { get; set; } = "pending";
        public decimal Subtotal { get; set; } = 0m;
        public decimal Discount { get; set; } = 0m;
        public decimal TotalAmount { get; set; } = 0m;
        public int? PromotionId { get; set; } = null;
        public string? Note { get; set; } = null;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Thông tin mở rộng (hiển thị)
        public string? CustomerName { get; set; }
        public string? UserName { get; set; }
        public string? PromotionCode { get; set; }
    }
}
