namespace StoreApp.Shared
{
    public class PromotionDTO
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Type { get; set; } = "percent"; // "percent" | "fixed"
        public decimal Value { get; set; }
        public decimal MinOrderAmount { get; set; }
        public decimal? MaxDiscount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public bool Active { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
