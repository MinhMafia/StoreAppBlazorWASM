 namespace StoreApp.Services.AI
 {
     /// <summary>
     /// Constants cho tên các Customer AI tools
     /// </summary>
     public static class CustomerAiToolNames
     {
         public const string SearchProducts = "search_products";
         public const string GetProductDetail = "get_product_detail";
         public const string GetCategories = "get_categories";
         public const string CheckPromotion = "check_promotion";
         public const string GetMyOrders = "get_my_orders";
         public const string GetOrderDetail = "get_order_detail";
 
         /// <summary>
         /// Lấy display name tiếng Việt cho tool
         /// </summary>
         public static string GetDisplayName(string toolName) => toolName switch
         {
             SearchProducts => "tìm sản phẩm",
             GetProductDetail => "chi tiết sản phẩm",
             GetCategories => "danh mục",
             CheckPromotion => "khuyến mãi",
             GetMyOrders => "đơn hàng của bạn",
             GetOrderDetail => "chi tiết đơn hàng",
             _ => toolName
         };
 
         /// <summary>
         /// Danh sách tất cả tool names
         /// </summary>
         public static readonly string[] All = new[]
         {
             SearchProducts,
             GetProductDetail,
             GetCategories,
             CheckPromotion,
             GetMyOrders,
             GetOrderDetail
         };
     }
 }
