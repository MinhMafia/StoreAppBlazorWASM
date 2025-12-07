 using System.Text.Json;
 using System.Security.Cryptography;
 using System.Text;
 using Microsoft.Extensions.Caching.Memory;
 
 namespace StoreApp.Services.AI
 {
     /// <summary>
     /// Thực thi các Customer AI tools
     /// Giới hạn quyền: chỉ xem sản phẩm, khuyến mãi, và đơn hàng của chính mình
     /// </summary>
     public class CustomerAiToolExecutor
     {
         private readonly IServiceProvider _serviceProvider;
         private readonly IMemoryCache _cache;
         private readonly ILogger<CustomerAiToolExecutor> _logger;
         private readonly ChatContextManager _contextManager;
 
         public CustomerAiToolExecutor(
             IServiceProvider serviceProvider,
             IMemoryCache cache,
             ILogger<CustomerAiToolExecutor> logger,
             ChatContextManager contextManager)
         {
             _serviceProvider = serviceProvider;
             _cache = cache;
             _logger = logger;
             _contextManager = contextManager;
         }
 
         /// <summary>
         /// Execute tool với customerId để đảm bảo chỉ xem được data của mình
         /// </summary>
         public async Task<string> ExecuteAsync(string toolCallId, string functionName, string argumentsJson, int? customerId = null)
         {
             try
             {
                 var args = string.IsNullOrEmpty(argumentsJson)
                     ? null
                     : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
 
                 _logger.LogInformation("Customer executing tool: {ToolName}", functionName);
 
                 using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(AiConstants.ToolTimeoutSeconds));
                 using var scope = _serviceProvider.CreateScope();
 
                 var result = await ExecuteFunctionAsync(functionName, args, scope.ServiceProvider, customerId, cts.Token);
 
                 var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });
                 json = _contextManager.TruncateToolResult(json, AiConstants.MaxToolResultTokens);
 
                 return json;
             }
             catch (OperationCanceledException)
             {
                 _logger.LogWarning("Customer tool {ToolName} timed out", functionName);
                 return JsonSerializer.Serialize(new { error = $"Tool '{functionName}' timeout." });
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Error executing customer tool {ToolName}", functionName);
                 return JsonSerializer.Serialize(new { error = $"Lỗi: {ex.Message}" });
             }
         }
 
         /// <summary>
         /// Execute multiple tools in parallel
         /// </summary>
         public async Task<List<(string toolCallId, string result)>> ExecuteParallelAsync(
             IEnumerable<(string id, string functionName, string arguments)> toolCalls,
             int? customerId = null)
         {
             var tasks = toolCalls.Select(async tc =>
             {
                 var result = await ExecuteAsync(tc.id, tc.functionName, tc.arguments, customerId);
                 return (tc.id, result);
             });
 
             var results = await Task.WhenAll(tasks);
             return results.ToList();
         }
 
         private async Task<object> ExecuteFunctionAsync(
             string functionName,
             Dictionary<string, JsonElement>? args,
             IServiceProvider sp,
             int? customerId,
             CancellationToken ct)
         {
             var cacheKey = GenerateSafeCacheKey(functionName, args, customerId);
 
             if (_cache.TryGetValue(cacheKey, out object? cachedResult) && cachedResult != null)
             {
                 _logger.LogDebug("Cache hit for customer tool {ToolName}", functionName);
                 return cachedResult;
             }
 
             var result = functionName switch
             {
                 CustomerAiToolNames.SearchProducts => await ExecuteSearchProductsAsync(args, sp, ct),
                 CustomerAiToolNames.GetProductDetail => await ExecuteGetProductDetailAsync(args, sp, ct),
                 CustomerAiToolNames.GetCategories => await ExecuteGetCategoriesAsync(sp, ct),
                 CustomerAiToolNames.CheckPromotion => await ExecuteCheckPromotionAsync(args, sp, ct),
                 CustomerAiToolNames.GetMyOrders => await ExecuteGetMyOrdersAsync(args, sp, customerId, ct),
                 CustomerAiToolNames.GetOrderDetail => await ExecuteGetOrderDetailAsync(args, sp, customerId, ct),
                 _ => new { error = $"Function '{functionName}' không được hỗ trợ" }
             };
 
             _cache.Set(cacheKey, result, TimeSpan.FromSeconds(AiConstants.ToolCacheDurationSeconds));
             return result;
         }
 
         private static string GenerateSafeCacheKey(string functionName, Dictionary<string, JsonElement>? args, int? customerId)
         {
             if (!CustomerAiToolNames.All.Contains(functionName))
             {
                 return $"customer_ai_invalid_{functionName.GetHashCode()}";
             }
 
             var argsJson = args != null ? JsonSerializer.Serialize(args) : "";
             var inputBytes = Encoding.UTF8.GetBytes($"{functionName}:{argsJson}:{customerId}");
             var hashBytes = SHA256.HashData(inputBytes);
             var hash = Convert.ToHexString(hashBytes)[..16];
 
             return $"customer_ai_{functionName}_{hash}";
         }
 
         #region Tool Implementations
 
         private async Task<object> ExecuteSearchProductsAsync(
             Dictionary<string, JsonElement>? args, IServiceProvider sp, CancellationToken ct)
         {
             var productService = sp.GetRequiredService<ProductService>();
 
             var keyword = ArgHelper.GetString(args, "keyword");
             var categoryId = ArgHelper.GetNullableInt(args, "category_id");
             var minPrice = ArgHelper.GetNullableDecimal(args, "min_price");
             var maxPrice = ArgHelper.GetNullableDecimal(args, "max_price");
             var sortBy = ArgHelper.GetString(args, "sort_by");
             var page = ArgHelper.GetInt(args, "page", 1);
             var limit = Math.Min(ArgHelper.GetInt(args, "limit", 10), 20);
 
             var result = await productService.GetPaginatedProductsAsync(
                 page: page,
                 pageSize: limit,
                 search: keyword,
                 categoryId: categoryId,
                 supplierId: null,
                 minPrice: minPrice,
                 maxPrice: maxPrice,
                 sortBy: sortBy ?? "",
                 status: 1 // Chỉ lấy sản phẩm active
             ).WaitAsync(ct);
 
             // Chỉ lấy sản phẩm còn hàng
             var items = result.Items
                 .Where(p => p.Inventory != null && p.Inventory.Quantity > 0)
                 .ToList();
 
             return new
             {
                 total = items.Count,
                 page,
                 products = items.Select(p => new
                 {
                     p.Id,
                     Name = p.ProductName,
                     p.Price,
                     CategoryName = p.Category?.Name,
                     InStock = p.Inventory?.Quantity > 0,
                     ImageUrl = p.ImageUrl
                 })
             };
         }
 
         private async Task<object> ExecuteGetProductDetailAsync(
             Dictionary<string, JsonElement>? args, IServiceProvider sp, CancellationToken ct)
         {
             var productService = sp.GetRequiredService<ProductService>();
 
             var productId = ArgHelper.GetNullableInt(args, "product_id");
             var productName = ArgHelper.GetString(args, "product_name");
 
             if (productId.HasValue)
             {
                var product = await productService.GetProductByIdAsync(productId.Value).WaitAsync(ct);
                 if (product == null)
                     return new { error = $"Không tìm thấy sản phẩm với ID {productId}" };
 
                 return new
                 {
                     product = new
                     {
                         product.Id,
                         Name = product.ProductName,
                         product.Description,
                         product.Price,
                         CategoryName = product.Category?.Name,
                         InStock = product.Inventory?.Quantity > 0,
                         Quantity = product.Inventory?.Quantity ?? 0,
                         ImageUrl = product.ImageUrl
                     }
                 };
             }
 
             if (!string.IsNullOrEmpty(productName))
             {
                 var result = await productService.GetPaginatedProductsAsync(
                     1, 5, productName, null, null, null, null, "", 1
                 ).WaitAsync(ct);
 
                 if (!result.Items.Any())
                     return new { error = $"Không tìm thấy sản phẩm '{productName}'" };
 
                 return new
                 {
                     products = result.Items.Select(p => new
                     {
                         p.Id,
                         Name = p.ProductName,
                         p.Description,
                         p.Price,
                         CategoryName = p.Category?.Name,
                         InStock = p.Inventory?.Quantity > 0
                     })
                 };
             }
 
             return new { error = "Vui lòng cung cấp product_id hoặc product_name" };
         }
 
         private async Task<object> ExecuteGetCategoriesAsync(IServiceProvider sp, CancellationToken ct)
         {
             var categoryService = sp.GetRequiredService<CategoryService>();
 
             var result = await categoryService.GetFilteredAndPaginatedAsync(
                 page: 1,
                 pageSize: 100,
                 keyword: null,
                 status: "active"
             ).WaitAsync(ct);
 
             return new
             {
                 categories = result.Items.Select(c => new
                 {
                     c.Id,
                     c.Name,
                     c.Description
                 })
             };
         }
 
         private async Task<object> ExecuteCheckPromotionAsync(
             Dictionary<string, JsonElement>? args, IServiceProvider sp, CancellationToken ct)
         {
             var promotionService = sp.GetRequiredService<PromotionService>();
 
             var code = ArgHelper.GetString(args, "code");
             var listActive = ArgHelper.GetNullableBool(args, "list_active") ?? false;
 
             if (!string.IsNullOrEmpty(code))
             {
                 var promotion = await promotionService.GetPromotionByCodeAsync(code).WaitAsync(ct);
                 if (promotion == null)
                     return new { valid = false, error = $"Mã '{code}' không tồn tại" };
 
                 var isExpired = promotion.EndDate < DateTime.UtcNow;
                 var isUsedUp = promotion.UsageLimit.HasValue && promotion.UsedCount >= promotion.UsageLimit;
 
                 return new
                 {
                     valid = promotion.Active && !isExpired && !isUsedUp,
                     promotion = new
                     {
                         promotion.Code,
                         promotion.Description,
                         DiscountType = promotion.Type,
                         DiscountValue = promotion.Value,
                         MinOrderValue = promotion.MinOrderAmount,
                         MaxDiscount = promotion.MaxDiscount,
                         promotion.EndDate,
                         IsExpired = isExpired,
                         IsUsedUp = isUsedUp
                     }
                 };
             }
 
             if (listActive)
             {
                 var result = await promotionService.GetPaginatedPromotionsAsync(
                     page: 1,
                     pageSize: 10,
                     search: null,
                     status: "active",
                     type: null
                 ).WaitAsync(ct);
 
                 var activePromotions = result.Items
                     .Where(p => p.EndDate >= DateTime.UtcNow)
                     .ToList();
 
                 return new
                 {
                     promotions = activePromotions.Select(p => new
                     {
                         p.Code,
                         p.Description,
                         DiscountType = p.Type,
                         DiscountValue = p.Value,
                         MinOrderValue = p.MinOrderAmount,
                         p.EndDate
                     })
                 };
             }
 
             return new { error = "Vui lòng cung cấp mã khuyến mãi hoặc đặt list_active = true" };
         }
 
         private async Task<object> ExecuteGetMyOrdersAsync(
             Dictionary<string, JsonElement>? args, IServiceProvider sp, int? customerId, CancellationToken ct)
         {
             if (!customerId.HasValue)
                 return new { error = "Bạn cần đăng nhập để xem đơn hàng" };
 
             var orderService = sp.GetRequiredService<OrderService>();
 
             var status = ArgHelper.GetString(args, "status");
             var page = ArgHelper.GetInt(args, "page", 1);
             var limit = Math.Min(ArgHelper.GetInt(args, "limit", 10), 10);
 
             var result = await orderService.GetOrdersByCustomerIdAsync(
                 customerId.Value, page, limit, status
             ).WaitAsync(ct);
 
             return new
             {
                 total = result.TotalItems,
                 page,
                 orders = result.Items.Select(o => new
                 {
                     o.Id,
                     o.OrderNumber,
                     o.Status,
                     o.TotalAmount,
                     o.CreatedAt
                 })
             };
         }
 
         private async Task<object> ExecuteGetOrderDetailAsync(
             Dictionary<string, JsonElement>? args, IServiceProvider sp, int? customerId, CancellationToken ct)
         {
             if (!customerId.HasValue)
                 return new { error = "Bạn cần đăng nhập để xem đơn hàng" };
 
             var orderService = sp.GetRequiredService<OrderService>();
 
             var orderId = ArgHelper.GetNullableInt(args, "order_id");
             var orderNumber = ArgHelper.GetString(args, "order_number");
 
             if (!orderId.HasValue && string.IsNullOrEmpty(orderNumber))
                 return new { error = "Vui lòng cung cấp order_id hoặc order_number" };
 
             var order = orderId.HasValue
                 ? await orderService.MapToDTOAsync(orderId.Value).WaitAsync(ct)
                 : await orderService.GetByOrderNumberAsync(orderNumber!).WaitAsync(ct);
 
             if (order == null)
                 return new { error = "Không tìm thấy đơn hàng" };
 
             // Kiểm tra ownership
             if (order.CustomerId != customerId)
                 return new { error = "Bạn không có quyền xem đơn hàng này" };
 
             return new
             {
                 order = new
                 {
                     order.Id,
                     order.OrderNumber,
                     order.Status,
                     order.Subtotal,
                     order.Discount,
                     order.TotalAmount,
                    order.CreatedAt
                 }
             };
         }
 
         #endregion
     }
 }
