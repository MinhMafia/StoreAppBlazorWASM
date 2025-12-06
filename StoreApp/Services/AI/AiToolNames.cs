namespace StoreApp.Services.AI
{
    /// <summary>
    /// Constants cho tên các AI tools - tránh magic strings
    /// </summary>
    public static class AiToolNames
    {
        public const string QueryProducts = "query_products";
        public const string QueryCategories = "query_categories";
        public const string QueryCustomers = "query_customers";
        public const string QueryOrders = "query_orders";
        public const string QueryPromotions = "query_promotions";
        public const string QuerySuppliers = "query_suppliers";
        public const string GetStatistics = "get_statistics";
        public const string GetReports = "get_reports";
        public const string GetInventoryStatus = "get_inventory_status";

        /// <summary>
        /// Lấy display name tiếng Việt cho tool
        /// </summary>
        public static string GetDisplayName(string toolName) => toolName switch
        {
            QueryProducts => "sản phẩm",
            QueryCategories => "danh mục",
            QueryCustomers => "khách hàng",
            QueryOrders => "đơn hàng",
            QueryPromotions => "khuyến mãi",
            QuerySuppliers => "nhà cung cấp",
            GetStatistics => "thống kê",
            GetReports => "báo cáo",
            GetInventoryStatus => "tồn kho",
            _ => toolName
        };

        /// <summary>
        /// Danh sách tất cả tool names
        /// </summary>
        public static readonly string[] All = new[]
        {
            QueryProducts,
            QueryCategories,
            QueryCustomers,
            QueryOrders,
            QueryPromotions,
            QuerySuppliers,
            GetStatistics,
            GetReports,
            GetInventoryStatus
        };
    }
}
