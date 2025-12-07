 using OpenAI.Chat;
 
 namespace StoreApp.Services.AI
 {
     /// <summary>
     /// Tool definitions cho Customer AI - giới hạn quyền truy cập
     /// Chỉ cho phép: tìm sản phẩm, xem đơn hàng của mình, kiểm tra khuyến mãi
     /// </summary>
     public static class CustomerAiToolDefinitions
     {
         /// <summary>
         /// Lấy tất cả tool definitions cho Customer
         /// </summary>
         public static List<ChatTool> GetAll()
         {
             return new List<ChatTool>
             {
                 CreateSearchProductsTool(),
                 CreateGetProductDetailTool(),
                 CreateGetCategoriesTool(),
                 CreateCheckPromotionTool(),
                 CreateGetMyOrdersTool(),
                 CreateGetOrderDetailTool()
             };
         }
 
         #region Tool Creators
 
         private static ChatTool CreateSearchProductsTool()
         {
             return ChatTool.CreateFunctionTool(
                 functionName: CustomerAiToolNames.SearchProducts,
                 functionDescription: "Tìm kiếm sản phẩm theo tên, danh mục hoặc giá. Dùng để giúp khách hàng tìm sản phẩm phù hợp.",
                 functionParameters: BinaryData.FromString("""
                 {
                     "type": "object",
                     "properties": {
                         "keyword": { "type": "string", "description": "Từ khóa tìm kiếm (tên sản phẩm)" },
                         "category_id": { "type": "integer", "description": "Lọc theo danh mục" },
                         "min_price": { "type": "number", "description": "Giá tối thiểu (VND)" },
                         "max_price": { "type": "number", "description": "Giá tối đa (VND)" },
                         "sort_by": { "type": "string", "enum": ["price_asc", "price_desc", "name_asc", "newest"], "description": "Sắp xếp kết quả" },
                         "page": { "type": "integer", "description": "Số trang (mặc định 1)" },
                         "limit": { "type": "integer", "description": "Số kết quả/trang (mặc định 10, tối đa 20)" }
                     },
                     "required": [],
                     "additionalProperties": false
                 }
                 """)
             );
         }
 
         private static ChatTool CreateGetProductDetailTool()
         {
             return ChatTool.CreateFunctionTool(
                 functionName: CustomerAiToolNames.GetProductDetail,
                 functionDescription: "Lấy thông tin chi tiết của một sản phẩm theo ID hoặc tên.",
                 functionParameters: BinaryData.FromString("""
                 {
                     "type": "object",
                     "properties": {
                         "product_id": { "type": "integer", "description": "ID sản phẩm" },
                         "product_name": { "type": "string", "description": "Tên sản phẩm (tìm gần đúng)" }
                     },
                     "required": [],
                     "additionalProperties": false
                 }
                 """)
             );
         }
 
         private static ChatTool CreateGetCategoriesTool()
         {
             return ChatTool.CreateFunctionTool(
                 functionName: CustomerAiToolNames.GetCategories,
                 functionDescription: "Lấy danh sách các danh mục sản phẩm đang có.",
                 functionParameters: BinaryData.FromString("""
                 {
                     "type": "object",
                     "properties": {},
                     "required": [],
                     "additionalProperties": false
                 }
                 """)
             );
         }
 
         private static ChatTool CreateCheckPromotionTool()
         {
             return ChatTool.CreateFunctionTool(
                 functionName: CustomerAiToolNames.CheckPromotion,
                 functionDescription: "Kiểm tra mã khuyến mãi có hợp lệ không, hoặc xem các khuyến mãi đang có.",
                 functionParameters: BinaryData.FromString("""
                 {
                     "type": "object",
                     "properties": {
                         "code": { "type": "string", "description": "Mã khuyến mãi cần kiểm tra" },
                         "list_active": { "type": "boolean", "description": "true = liệt kê tất cả khuyến mãi đang hoạt động" }
                     },
                     "required": [],
                     "additionalProperties": false
                 }
                 """)
             );
         }
 
         private static ChatTool CreateGetMyOrdersTool()
         {
             return ChatTool.CreateFunctionTool(
                 functionName: CustomerAiToolNames.GetMyOrders,
                 functionDescription: "Xem danh sách đơn hàng của khách hàng. Chỉ xem được đơn hàng của chính mình.",
                 functionParameters: BinaryData.FromString("""
                 {
                     "type": "object",
                     "properties": {
                         "status": { "type": "string", "enum": ["pending", "completed", "cancelled"], "description": "Lọc theo trạng thái" },
                         "page": { "type": "integer", "description": "Số trang" },
                         "limit": { "type": "integer", "description": "Số kết quả/trang (tối đa 10)" }
                     },
                     "required": [],
                     "additionalProperties": false
                 }
                 """)
             );
         }
 
         private static ChatTool CreateGetOrderDetailTool()
         {
             return ChatTool.CreateFunctionTool(
                 functionName: CustomerAiToolNames.GetOrderDetail,
                 functionDescription: "Xem chi tiết một đơn hàng cụ thể. Chỉ xem được đơn hàng của chính mình.",
                 functionParameters: BinaryData.FromString("""
                 {
                     "type": "object",
                     "properties": {
                         "order_id": { "type": "integer", "description": "ID đơn hàng" },
                         "order_number": { "type": "string", "description": "Mã đơn hàng" }
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
