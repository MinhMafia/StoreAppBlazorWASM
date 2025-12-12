// StoreApp.Shared/DTO/ProductDTO.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StoreApp.Shared
{
    public class ProductDTO
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string? Sku { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tên sản phẩm không được quá 255 ký tự")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int? CategoryId { get; set; }

        public int? SupplierId { get; set; }

        [Required(ErrorMessage = "Giá bán là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá vốn phải lớn hơn hoặc bằng 0")]
        public decimal? Cost { get; set; }

        public int? UnitId { get; set; }

        public string? Unit { get; set; }

        [StringLength(2000, ErrorMessage = "Mô tả không được quá 2000 ký tự")]
        public string? Description { get; set; }

        [StringLength(1024)]
        // BỎ [Url] validation vì đường dẫn local không phải URL đầy đủ
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public CategoryDTO? Category { get; set; }
        public SupplierDTO? Supplier { get; set; }
        public InventoryDTO? Inventory { get; set; }

        public double AverageRating { get; set; } = 0.0;
        public int ReviewCount { get; set; } = 0;
    }
}