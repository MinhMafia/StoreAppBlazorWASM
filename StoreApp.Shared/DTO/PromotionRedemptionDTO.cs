namespace StoreApp.Shared
{
    public class PromotionRedemptionDTO
    {
        public int Id { get; set; }
        public int PromotionId { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public decimal OrderAmount { get; set; }
        public DateTime RedeemedAt { get; set; }
    }
}
