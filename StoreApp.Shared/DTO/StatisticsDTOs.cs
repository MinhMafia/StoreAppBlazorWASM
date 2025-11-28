namespace StoreApp.Shared
{
    public class RevenueDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class OrderStatsDTO
    {
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class ProductSalesDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public string? ImageUrl { get; set; }
        public string? CategoryName { get; set; }
    }

    public class ProductInventoryDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime LastCheckedAt { get; set; }
    }
}
