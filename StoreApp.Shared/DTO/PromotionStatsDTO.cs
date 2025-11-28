namespace StoreApp.Shared
{
    public class PromotionStatsDTO
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int TotalRedemptions { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public int UniqueCustomers { get; set; }
        public decimal AverageOrderValue { get; set; }
    }
}
