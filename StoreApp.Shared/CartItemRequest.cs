namespace StoreApp.Shared
{
    /// <summary>
    /// Minimal cart payload persisted in storage and synced with server.
    /// Only keeps identifiers and quantities to avoid stale product metadata.
    /// </summary>
    public class CartItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
