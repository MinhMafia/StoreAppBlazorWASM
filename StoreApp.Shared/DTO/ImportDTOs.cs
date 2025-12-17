namespace StoreApp.Shared
{
    /// <summary>
    /// DTO cho danh sách phiếu nhập
    /// </summary>
    public class ImportListItemDTO
    {
        public int Id { get; set; }
        public string ImportNumber { get; set; } = string.Empty;
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int? StaffId { get; set; }
        public string? StaffName { get; set; }
        public string Status { get; set; } = "pending";
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }  // Tổng số loại sản phẩm
        public int TotalQuantity { get; set; }  // Tổng số lượng
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO cho chi tiết phiếu nhập
    /// </summary>
    public class ImportDetailDTO
    {
        public int Id { get; set; }
        public string ImportNumber { get; set; } = string.Empty;
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierPhone { get; set; }
        public string? SupplierAddress { get; set; }
        public int? StaffId { get; set; }
        public string? StaffName { get; set; }
        public string Status { get; set; } = "pending";
        public decimal TotalAmount { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Chi tiết các sản phẩm trong phiếu
        public List<ImportItemDetailDTO> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO cho chi tiết từng sản phẩm trong phiếu nhập
    /// </summary>
    public class ImportItemDetailDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSku { get; set; }
        public string? UnitName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }

    /// <summary>
    /// DTO để tạo phiếu nhập mới
    /// </summary>
    public class CreateImportDTO
    {
        public int? SupplierId { get; set; }
        public int? StaffId { get; set; }
        public string? Note { get; set; }
        public List<CreateImportItemDTO> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO cho sản phẩm trong phiếu nhập mới
    /// </summary>
    public class CreateImportItemDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty; // THÊM để hiển thị trong UI
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}
