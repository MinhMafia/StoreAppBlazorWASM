namespace StoreApp.Shared
{
    public class ProductImportDTO
    {
        public string? Sku { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public decimal Price { get; set; }
        public decimal? Cost { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

