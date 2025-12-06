using OpenAI.Chat;

namespace StoreApp.Services.AI
{
    /// <summary>
    /// Tool definitions cho AI - tách riêng để dễ maintain và modify
    /// Có thể chuyển sang load từ JSON file trong tương lai
    /// </summary>
    public static class AiToolDefinitions
    {
        /// <summary>
        /// Lấy tất cả tool definitions cho ChatCompletion
        /// </summary>
        public static List<ChatTool> GetAll()
        {
            return new List<ChatTool>
            {
                CreateQueryProductsTool(),
                CreateQueryCategoriesTool(),
                CreateQueryCustomersTool(),
                CreateQueryOrdersTool(),
                CreateQueryPromotionsTool(),
                CreateQuerySuppliersTool(),
                CreateGetStatisticsTool(),
                CreateGetReportsTool(),
                CreateGetInventoryStatusTool()
            };
        }

        #region Tool Creators

        private static ChatTool CreateQueryProductsTool()
        {
            return ChatTool.CreateFunctionTool(
                functionName: AiToolNames.QueryProducts,
                functionDescription: "Tìm kiếm và lọc sản phẩm theo nhiều tiêu chí. Hỗ trợ pagination.",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "keyword": { "type": "string", "description": "Từ khóa tìm theo tên sản phẩm" },
                        "category_id": { "type": "integer", "description": "Lọc theo ID danh mục" },
                        "supplier_id": { "type": "integer", "description": "Lọc theo ID nhà cung cấp" },
                        "min_price": { "type": "number", "description": "Giá tối thiểu" },
                        "max_price": { "type": "number", "description": "Giá tối đa" },
                        "in_stock": { "type": "boolean", "description": "true=còn hàng, false=hết hàng" },
                        "is_active": { "type": "boolean", "description": "Trạng thái hoạt động" },
                        "page": { "type": "integer", "description": "Số trang (mặc định 1)" },
                        "limit": { "type": "integer", "description": "Số kết quả/trang (mặc định 20, tối đa 50)" },
                        "sort_by": { "type": "string", "description": "Sắp xếp: price_asc, price_desc, name_asc, name_desc" }
                    },
                    "required": [],
                    "additionalProperties": false
                }
                """)
            );
        }

        private static ChatTool CreateQueryCategoriesTool()
        {
            return ChatTool.CreateFunctionTool(
                functionName: AiToolNames.QueryCategories,
                functionDescription: "Lấy danh sách danh mục sản phẩm",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "keyword": { "type": "string", "description": "Tìm theo tên danh mục" },
                        "is_active": { "type": "boolean", "description": "Lọc theo trạng thái" },
                        "page": { "type": "integer", "description": "Số trang" },
                        "limit": { "type": "integer", "description": "Số kết quả/trang" }
                    },
                    "required": [],
                    "additionalProperties": false
                }
                """)
            );
        }

        private static ChatTool CreateQueryCustomersTool()
        {
            return ChatTool.CreateFunctionTool(
                functionName: AiToolNames.QueryCustomers,
                functionDescription: "Tìm kiếm khách hàng theo tên, SĐT, email",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "keyword": { "type": "string", "description": "Tìm theo tên, email, SĐT" },
                        "is_active": { "type": "boolean", "description": "Lọc theo trạng thái" },
                        "page": { "type": "integer", "description": "Số trang" },
                        "limit": { "type": "integer", "description": "Số kết quả/trang" }
                    },
                    "required": [],
                    "additionalProperties": false
                }
                """)
            );
        }

        private static ChatTool CreateQueryOrdersTool()
        {
            return ChatTool.CreateFunctionTool(
                functionName: AiToolNames.QueryOrders,
                functionDescription: "Tìm kiếm đơn hàng. Xem chi tiết bằng order_id.",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "order_id": { "type": "integer", "description": "ID đơn hàng cụ thể" },
                        "status": { "type": "string", "enum": ["pending", "completed", "cancelled"], "description": "Trạng thái đơn" },
                        "date_from": { "type": "string", "description": "Từ ngày (yyyy-MM-dd)" },
                        "date_to": { "type": "string", "description": "Đến ngày (yyyy-MM-dd)" },
                        "keyword": { "type": "string", "description": "Tìm theo tên/SĐT khách" },
                        "page": { "type": "integer", "description": "Số trang" },
                        "limit": { "type": "integer", "description": "Số kết quả/trang" }
                    },
                    "required": [],
                    "additionalProperties": false
                }
                """)
            );
        }

        private static ChatTool CreateQueryPromotionsTool()
        {
            return ChatTool.CreateFunctionTool(
                functionName: AiToolNames.QueryPromotions,
                functionDescription: "Tìm kiếm khuyến mãi. Kiểm tra mã cụ thể bằng code.",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "keyword": { "type": "string", "description": "Tìm theo tên khuyến mãi" },
                        "code": { "type": "string", "description": "Mã khuyến mãi cụ thể" },
                        "status": { "type": "string", "enum": ["active", "inactive", "expired"], "description": "Trạng thái" },
                        "type": { "type": "string", "enum": ["percent", "fixed"], "description": "Loại giảm giá" },
                        "page": { "type": "integer", "description": "Số trang" },
                        "limit": { "type": "integer", "description": "Số kết quả/trang" }
                    },
                    "required": [],
                    "additionalProperties": false
                }
                """)
            );
        }

        private static ChatTool CreateQuerySuppliersTool()
        {
            return ChatTool.CreateFunctionTool(
                functionName: AiToolNames.QuerySuppliers,
                functionDescription: "Lấy danh sách nhà cung cấp",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "keyword": { "type": "string", "description": "Tìm theo tên NCC" },
                        "page": { "type": "integer", "description": "Số trang" },
                        "limit": { "type": "integer", "description": "Số kết quả/trang" }
                    },
                    "required": [],
                    "additionalProperties": false
                }
                """)
            );
        }

        private static ChatTool CreateGetStatisticsTool()
        {
            return ChatTool.CreateFunctionTool(
                functionName: AiToolNames.GetStatistics,
                functionDescription: "Lấy thống kê: tổng quan, doanh thu, bán chạy, tồn kho thấp",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "type": {
                            "type": "string",
                            "enum": ["overview", "revenue", "best_sellers", "low_stock", "order_stats"],
                            "description": "Loại thống kê"
                        },
                        "days": { "type": "integer", "description": "Số ngày (mặc định 7)" },
                        "limit": { "type": "integer", "description": "Số kết quả" },
                        "threshold": { "type": "integer", "description": "Ngưỡng tồn kho" }
                    },
                    "required": ["type"],
                    "additionalProperties": false
                }
                """)
            );
        }

        private static ChatTool CreateGetReportsTool()
        {
            return ChatTool.CreateFunctionTool(
                functionName: AiToolNames.GetReports,
                functionDescription: "Lấy báo cáo chi tiết theo khoảng thời gian",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "type": {
                            "type": "string",
                            "enum": ["sales_summary", "top_products", "top_customers", "revenue_by_day"],
                            "description": "Loại báo cáo"
                        },
                        "date_from": { "type": "string", "description": "Từ ngày (yyyy-MM-dd)" },
                        "date_to": { "type": "string", "description": "Đến ngày (yyyy-MM-dd)" },
                        "limit": { "type": "integer", "description": "Số kết quả" }
                    },
                    "required": ["type"],
                    "additionalProperties": false
                }
                """)
            );
        }

        private static ChatTool CreateGetInventoryStatusTool()
        {
            return ChatTool.CreateFunctionTool(
                functionName: AiToolNames.GetInventoryStatus,
                functionDescription: "Kiểm tra tình trạng tồn kho tổng quan",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "threshold": { "type": "integer", "description": "Ngưỡng cảnh báo (mặc định 10)" },
                        "category_id": { "type": "integer", "description": "Lọc theo danh mục" }
                    },
                    "required": [],
                    "additionalProperties": false
                }
                """)
            );
        }

        #endregion
    }
}
