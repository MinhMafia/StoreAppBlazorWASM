namespace StoreApp.Shared.DTO
{
    public class SalesReportDTO
    {
        public DateTime Date { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal OrderTotal { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class InventoryReportDTO
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? CategoryName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class SalesSummaryDTO
    {
        public decimal NetRevenue { get; set; }
        public decimal TotalDiscount { get; set; }
        public int TotalOrders { get; set; }
        public int ProductsSold { get; set; }
    }

    public class RevenueByDayDTO
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class HighValueInventoryDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public int Quantity { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class PeriodComparisonDTO
    {
        public SalesSummaryDTO CurrentPeriod { get; set; } = new SalesSummaryDTO();
        public SalesSummaryDTO PreviousPeriod { get; set; } = new SalesSummaryDTO();
        public decimal RevenueChangePercent { get; set; }
        public decimal OrdersChangePercent { get; set; }
        public decimal ProductsSoldChangePercent { get; set; }
    }

    public class TopProductReportDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? CategoryName { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal AveragePrice { get; set; }
    }

    public class TopCustomerReportDTO
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class SalesByStaffDTO
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
    }
}

