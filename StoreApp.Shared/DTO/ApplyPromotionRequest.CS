namespace StoreApp.Shared
{
        public class ApplyPromotionRequest
    {
        public int PromotionId { get; set; }
        public int OrderId { get; set; }
        public int? CustomerId { get; set; }
    }
}