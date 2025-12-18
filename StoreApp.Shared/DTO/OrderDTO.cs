namespace  StoreApp.Shared
{
    public class OrderDTO
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int? CustomerId { get; set; } = 0;
        public int? StaffId { get; set; }  // Đổi từ UserId
        public string Status { get; set; } = "pending";
        public decimal Subtotal { get; set; } = 0m;
        public decimal Discount { get; set; } = 0m;
        public decimal TotalAmount { get; set; } = 0m;
        public int? PromotionId { get; set; } = null;
        public string? Note { get; set; } = null;
        public string? ShippingAddress { get; set; } = null;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Thông tin mở rộng (hiển thị)
        public string? DiaChiKhachHang { get; set; }
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? CustomerName { get; set; }
        public string? StaffName { get; set; }  // Đổi từ UserName
        public string? PromotionCode { get; set; }

        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }

        public string? TransactionRef { get; set; }

        // Danh sách sản phẩm (để hỗ trợ tạo order từ frontend)
        public List<OrderItemDTO>? Items { get; set; }
    }

    public class OrderItemDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }
}
