namespace StoreApp.Shared
{
    /// <summary>
    /// Dòng dữ liệu cho bảng tồn kho (kết hợp Product + Inventory).
    /// </summary>
    public class InventoryListItemDTO
    {
        public int Id { get; set; }                 // inventory.id
        public int ProductId { get; set; }          // products.id
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? CategoryName { get; set; }
        public string? SupplierName { get; set; }
        public decimal Price { get; set; }
        public decimal? Cost { get; set; }
        public string? Unit { get; set; }
        public int Quantity { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Thống kê tổng quan tồn kho dùng cho 4 thẻ ở đầu trang.
    /// </summary>
    public class InventoryStatsDTO
    {
        public int Total { get; set; }       // tổng sản phẩm có trong bảng inventory
        public int OutOfStock { get; set; }  // quantity = 0
        public int LowStock { get; set; }    // 0 < quantity < threshold (mặc định 10)
        public int InStock { get; set; }     // quantity >= threshold
    }

    /// <summary>
    /// Payload khi điều chỉnh số lượng tồn kho thủ công.
    /// </summary>
    public class AdjustInventoryRequestDTO
    {
        public int InventoryId { get; set; }
        public int NewQuantity { get; set; }
        public string? Reason { get; set; }
        public decimal? NewCost { get; set; }        // THÊM MỚI - giá vốn mới
        public int ProductId { get; set; }
    }

    /// <summary>
    /// DTO cho sản phẩm có vấn đề về giá (cost > price)
    /// </summary>
    public class InvalidPriceProductDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? SupplierName { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public decimal PriceDifference { get; set; }  // Cost - Price
        public int Quantity { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Kết quả kiểm kê
    /// </summary>
    public class InventoryAuditResultDTO
    {
        public List<InvalidPriceProductDTO> InvalidProducts { get; set; } = new();
        public int TotalInvalid { get; set; }
        public int TotalDeactivated { get; set; }
    }
}


