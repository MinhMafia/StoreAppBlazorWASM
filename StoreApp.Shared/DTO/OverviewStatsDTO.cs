namespace StoreApp.Shared
{
    public class OverviewStatsDTO
    {
        public decimal TodayRevenue { get; set; }
        public decimal RevenueChange { get; set; }
        public int TodayOrders { get; set; }
        public int OrdersChange { get; set; }
        public int TodayProductsSold { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal TotalDiscountApplied { get; set; }
        public int LowStockCount { get; set; }
        public decimal InventoryValue { get; set; }
    }
}
