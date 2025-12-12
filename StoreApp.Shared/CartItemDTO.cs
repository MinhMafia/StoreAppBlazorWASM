namespace StoreApp.Shared
{
    public class CartItemDTO
    {
        public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int? AvailableQuantity { get; set; }
    public decimal Total => Price * Quantity;
    public bool IsOutOfStock => AvailableQuantity.HasValue && AvailableQuantity.Value <= 0;
    public bool CanIncreaseQuantity => !AvailableQuantity.HasValue || Quantity < AvailableQuantity.Value;
}
}
