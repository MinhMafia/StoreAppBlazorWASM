using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace StoreApp.Services.AI
{
    /// <summary>
    /// Thực thi các AI tools - tách từ AiService để giảm complexity
    /// Một implementation duy nhất cho cả sequential và parallel execution
    /// </summary>
    public class AiToolExecutor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AiToolExecutor> _logger;
        private readonly ChatContextManager _contextManager;

        public AiToolExecutor(
            IServiceProvider serviceProvider,
            IMemoryCache cache,
            ILogger<AiToolExecutor> logger,
            ChatContextManager contextManager)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
            _logger = logger;
            _contextManager = contextManager;
        }

        /// <summary>
        /// Execute tool với DI scope mới - dùng cho cả sequential và parallel
        /// </summary>
        public async Task<string> ExecuteAsync(string toolCallId, string functionName, string argumentsJson)
        {
            try
            {
                var args = string.IsNullOrEmpty(argumentsJson)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);

                _logger.LogInformation("Executing tool: {ToolName}", functionName);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(AiConstants.ToolTimeoutSeconds));

                // Tạo scope mới cho mỗi tool call - đảm bảo thread-safe DbContext
                using var scope = _serviceProvider.CreateScope();
                var result = await ExecuteFunctionAsync(functionName, args, scope.ServiceProvider, cts.Token);

                var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

                // Truncate để tránh chiếm quá nhiều context
                json = _contextManager.TruncateToolResult(json, AiConstants.MaxToolResultTokens);

                return json;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Tool {ToolName} timed out after {Timeout}s", functionName, AiConstants.ToolTimeoutSeconds);
                return JsonSerializer.Serialize(new { error = $"Tool '{functionName}' timeout sau {AiConstants.ToolTimeoutSeconds}s." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {ToolName}", functionName);
                return JsonSerializer.Serialize(new { error = $"Lỗi thực thi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Execute multiple tools in parallel
        /// </summary>
        public async Task<List<(string toolCallId, string result)>> ExecuteParallelAsync(
            IEnumerable<(string id, string functionName, string arguments)> toolCalls)
        {
            var tasks = toolCalls.Select(async tc =>
            {
                var result = await ExecuteAsync(tc.id, tc.functionName, tc.arguments);
                return (tc.id, result);
            });

            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        /// <summary>
        /// Execute function với cache
        /// Cache key được sanitize để tránh injection
        /// </summary>
        private async Task<object> ExecuteFunctionAsync(
            string functionName,
            Dictionary<string, JsonElement>? args,
            IServiceProvider sp,
            CancellationToken ct)
        {
            // Sanitize cache key - sử dụng hash thay vì raw input
            var cacheKey = GenerateSafeCacheKey(functionName, args);

            if (_cache.TryGetValue(cacheKey, out object? cachedResult) && cachedResult != null)
            {
                _logger.LogDebug("Cache hit for tool {ToolName}", functionName);
                return cachedResult;
            }

            var result = functionName switch
            {
                AiToolNames.QueryProducts => await ExecuteQueryProductsAsync(args, sp, ct),
                AiToolNames.QueryCategories => await ExecuteQueryCategoriesAsync(args, sp, ct),
                AiToolNames.QueryCustomers => await ExecuteQueryCustomersAsync(args, sp, ct),
                AiToolNames.QueryOrders => await ExecuteQueryOrdersAsync(args, sp, ct),
                AiToolNames.QueryPromotions => await ExecuteQueryPromotionsAsync(args, sp, ct),
                AiToolNames.QuerySuppliers => await ExecuteQuerySuppliersAsync(args, sp, ct),
                AiToolNames.GetStatistics => await ExecuteGetStatisticsAsync(args, sp, ct),
                AiToolNames.GetReports => await ExecuteGetReportsAsync(args, sp, ct),
                AiToolNames.GetInventoryStatus => await ExecuteGetInventoryStatusAsync(args, sp, ct),
                _ => new { error = $"Function '{functionName}' không được hỗ trợ" }
            };

            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(AiConstants.ToolCacheDurationSeconds));
            return result;
        }

        /// <summary>
        /// Generate safe cache key using hash to prevent injection
        /// </summary>
        private static string GenerateSafeCacheKey(string functionName, Dictionary<string, JsonElement>? args)
        {
            // Validate function name is in allowed list
            if (!AiToolNames.All.Contains(functionName))
            {
                return $"ai_tool_invalid_{functionName.GetHashCode()}";
            }

            var argsJson = args != null ? JsonSerializer.Serialize(args) : "";

            // Use SHA256 hash for cache key to prevent any injection
            var inputBytes = Encoding.UTF8.GetBytes($"{functionName}:{argsJson}");
            var hashBytes = SHA256.HashData(inputBytes);
            var hash = Convert.ToHexString(hashBytes)[..16]; // Take first 16 chars

            return $"ai_tool_{functionName}_{hash}";
        }

        #region Tool Implementations

        private async Task<object> ExecuteQueryProductsAsync(
            Dictionary<string, JsonElement>? args, IServiceProvider sp, CancellationToken ct)
        {
            var productService = sp.GetRequiredService<ProductService>();

            var keyword = ArgHelper.GetString(args, "keyword");
            var categoryId = ArgHelper.GetNullableInt(args, "category_id");
            var supplierId = ArgHelper.GetNullableInt(args, "supplier_id");
            var minPrice = ArgHelper.GetNullableDecimal(args, "min_price");
            var maxPrice = ArgHelper.GetNullableDecimal(args, "max_price");
            var inStock = ArgHelper.GetNullableBool(args, "in_stock");
            var isActive = ArgHelper.GetNullableBool(args, "is_active");
            var page = ArgHelper.GetInt(args, "page", 1);
            var limit = Math.Min(ArgHelper.GetInt(args, "limit", 20), 50);
            var sortBy = ArgHelper.GetString(args, "sort_by");

            int? status = isActive.HasValue ? (isActive.Value ? 1 : 0) : null;

            var result = await productService.GetPaginatedProductsAsync(
                page: page,
                pageSize: limit,
                search: keyword,
                categoryId: categoryId,
                supplierId: supplierId,
                minPrice: minPrice,
                maxPrice: maxPrice,
                sortBy: sortBy ?? "",
                status: status
            ).WaitAsync(ct);

            var items = result.Items;
            if (inStock.HasValue)
            {
                items = inStock.Value
                    ? items.Where(p => p.Inventory != null && p.Inventory.Quantity > 0).ToList()
                    : items.Where(p => p.Inventory == null || p.Inventory.Quantity <= 0).ToList();
            }

            return new
            {
                total = result.TotalItems,
                page,
                pageSize = limit,
                totalPages = (int)Math.Ceiling(result.TotalItems / (double)limit),
                hasMore = page * limit < result.TotalItems,
                products = items.Select(p => new
                {
                    p.Id,
                    Name = p.ProductName,
                    p.Sku,
                    p.Price,
                    CategoryName = p.Category?.Name,
                    SupplierName = p.Supplier?.Name,
                    Quantity = p.Inventory?.Quantity ?? 0,
                    Status = p.IsActive ? "active" : "inactive"
                })
            };
        }

        private async Task<object> ExecuteQueryCategoriesAsync(
            Dictionary<string, JsonElement>? args, IServiceProvider sp, CancellationToken ct)
        {
            var categoryService = sp.GetRequiredService<CategoryService>();

            var keyword = ArgHelper.GetString(args, "keyword");
            var isActive = ArgHelper.GetNullableBool(args, "is_active");
            var page = ArgHelper.GetInt(args, "page", 1);
            var limit = Math.Min(ArgHelper.GetInt(args, "limit", 50), 100);

            string? status = isActive.HasValue ? (isActive.Value ? "active" : "inactive") : null;

            var result = await categoryService.GetFilteredAndPaginatedAsync(
                page: page,
                pageSize: limit,
                keyword: keyword,
                status: status
            ).WaitAsync(ct);

            return new
            {
                total = result.TotalItems,
                page,
                totalPages = (int)Math.Ceiling(result.TotalItems / (double)limit),
                categories = result.Items.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    Status = c.IsActive ? "active" : "inactive"
                })
            };
        }

        private async Task<object> ExecuteQueryCustomersAsync(
            Dictionary<string, JsonElement>? args, IServiceProvider sp, CancellationToken ct)
        {
            var customerService = sp.GetRequiredService<CustomerService>();

            var keyword = ArgHelper.GetString(args, "keyword");
            var isActive = ArgHelper.GetNullableBool(args, "is_active");
            var page = ArgHelper.GetInt(args, "page", 1);
            var limit = Math.Min(ArgHelper.GetInt(args, "limit", 20), 50);

            string? status = isActive.HasValue ? (isActive.Value ? "active" : "inactive") : null;

            var result = await customerService.GetFilteredAndPaginatedAsync(
                page: page,
                pageSize: limit,
                keyword: keyword,
                status: status
            ).WaitAsync(ct);

            return new
            {
                total = result.TotalItems,
                page,
                totalPages = (int)Math.Ceiling(result.TotalItems / (double)limit),
                customers = result.Items.Select(c => new
                {
                    c.Id,
                    c.FullName,
                    c.Phone,
                    c.Email,
                    c.Address,
                    Status = c.IsActive ? "active" : "inactive"
                })
            };
        }

        private async Task<object> ExecuteQueryOrdersAsync(
            Dictionary<string, JsonElement>? args, IServiceProvider sp, CancellationToken ct)
        {
            var orderService = sp.GetRequiredService<OrderService>();

            var orderId = ArgHelper.GetNullableInt(args, "order_id");
            var status = ArgHelper.GetString(args, "status");
            var dateFromStr = ArgHelper.GetString(args, "date_from");
            var dateToStr = ArgHelper.GetString(args, "date_to");
            var keyword = ArgHelper.GetString(args, "keyword");
            var page = ArgHelper.GetInt(args, "page", 1);
            var limit = Math.Min(ArgHelper.GetInt(args, "limit", 20), 50);

            if (orderId.HasValue)
            {
                var orderDetail = await orderService.MapToDTOAsync(orderId.Value).WaitAsync(ct);
                if (orderDetail == null)
                    return new { error = $"Không tìm thấy đơn hàng #{orderId}" };

                return new
                {
                    order = new
                    {
                        orderDetail.Id,
                        orderDetail.OrderNumber,
                        orderDetail.CustomerName,
                        orderDetail.Status,
                        orderDetail.Subtotal,
                        orderDetail.Discount,
                        orderDetail.TotalAmount,
                        orderDetail.CreatedAt
                    }
                };
            }

            DateTime? startDate = null;
            DateTime? endDate = null;
            if (!string.IsNullOrEmpty(dateFromStr) && DateTime.TryParse(dateFromStr, out var df))
                startDate = df;
            if (!string.IsNullOrEmpty(dateToStr) && DateTime.TryParse(dateToStr, out var dt))
                endDate = dt;

            var result = await orderService.GetPagedOrdersAsync(
                pageNumber: page,
                pageSize: limit,
                status: status,
                startDate: startDate,
                endDate: endDate,
                search: keyword
            ).WaitAsync(ct);

            return new
            {
                total = result.TotalItems,
                page,
                totalPages = (int)Math.Ceiling(result.TotalItems / (double)limit),
                hasMore = page * limit < result.TotalItems,
                orders = result.Items.Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    o.CustomerName,
                    o.Status,
                    o.Subtotal,
                    o.Discount,
                    o.TotalAmount,
                    o.CreatedAt
                })
            };
        }

        private async Task<object> ExecuteQueryPromotionsAsync(
            Dictionary<string, JsonElement>? args, IServiceProvider sp, CancellationToken ct)
        {
            var promotionService = sp.GetRequiredService<PromotionService>();

            var keyword = ArgHelper.GetString(args, "keyword");
            var code = ArgHelper.GetString(args, "code");
            var status = ArgHelper.GetString(args, "status");
            var type = ArgHelper.GetString(args, "type");
            var page = ArgHelper.GetInt(args, "page", 1);
            var limit = Math.Min(ArgHelper.GetInt(args, "limit", 20), 50);

            if (!string.IsNullOrEmpty(code))
            {
                var promotion = await promotionService.GetPromotionByCodeAsync(code).WaitAsync(ct);
                if (promotion == null)
                    return new { error = $"Không tìm thấy khuyến mãi với mã '{code}'" };

                return new
                {
                    promotion = new
                    {
                        promotion.Id,
                        promotion.Code,
                        promotion.Description,
                        DiscountType = promotion.Type,
                        DiscountValue = promotion.Value,
                        MinOrderValue = promotion.MinOrderAmount,
                        MaxDiscountAmount = promotion.MaxDiscount,
                        promotion.StartDate,
                        promotion.EndDate,
                        promotion.UsageLimit,
                        promotion.UsedCount,
                        IsActive = promotion.Active,
                        IsExpired = promotion.EndDate < DateTime.UtcNow,
                        RemainingUses = promotion.UsageLimit.HasValue ? promotion.UsageLimit - promotion.UsedCount : null
                    }
                };
            }

            var result = await promotionService.GetPaginatedPromotionsAsync(
                page: page,
                pageSize: limit,
                search: keyword,
                status: status,
                type: type
            ).WaitAsync(ct);

            return new
            {
                total = result.TotalItems,
                page,
                totalPages = (int)Math.Ceiling(result.TotalItems / (double)limit),
                promotions = result.Items.Select(p => new
                {
                    p.Id,
                    p.Code,
                    p.Description,
                    DiscountType = p.Type,
                    DiscountValue = p.Value,
                    p.StartDate,
                    p.EndDate,
                    IsActive = p.Active,
                    IsExpired = p.EndDate < DateTime.UtcNow
                })
            };
        }

        private async Task<object> ExecuteQuerySuppliersAsync(
            Dictionary<string, JsonElement>? args, IServiceProvider sp, CancellationToken ct)
        {
            var supplierService = sp.GetRequiredService<SupplierService>();

            var keyword = ArgHelper.GetString(args, "keyword");
            var page = ArgHelper.GetInt(args, "page", 1);
            var limit = Math.Min(ArgHelper.GetInt(args, "limit", 50), 100);

            var result = await supplierService.GetPaginatedAsync(
                page: page,
                pageSize: limit,
                search: keyword
            ).WaitAsync(ct);

            return new
            {
                total = result.TotalItems,
                page,
                totalPages = (int)Math.Ceiling(result.TotalItems / (double)limit),
                suppliers = result.Items
            };
        }

        private async Task<object> ExecuteGetStatisticsAsync(
            Dictionary<string, JsonElement>? args, IServiceProvider sp, CancellationToken ct)
        {
            var statisticsService = sp.GetRequiredService<StatisticsService>();

            var type = ArgHelper.GetString(args, "type") ?? "overview";
            var days = ArgHelper.GetInt(args, "days", 7);
            var limit = Math.Min(ArgHelper.GetInt(args, "limit", 10), 50);
            var threshold = ArgHelper.GetInt(args, "threshold", 10);

            return type switch
            {
                "overview" => await statisticsService.GetOverviewStatsAsync().WaitAsync(ct),
                "revenue" => await statisticsService.GetRevenueByPeriodAsync(days).WaitAsync(ct),
                "best_sellers" => await statisticsService.GetBestSellersAsync(limit, days).WaitAsync(ct),
                "low_stock" => await statisticsService.GetLowStockProductsAsync(threshold).WaitAsync(ct),
                "order_stats" => await statisticsService.GetOrderStatsAsync(days).WaitAsync(ct),
                _ => new { error = $"Loại thống kê '{type}' không hỗ trợ. Dùng: overview, revenue, best_sellers, low_stock, order_stats" }
            };
        }

        private async Task<object> ExecuteGetReportsAsync(
            Dictionary<string, JsonElement>? args, IServiceProvider sp, CancellationToken ct)
        {
            var reportsService = sp.GetRequiredService<ReportsService>();

            var type = ArgHelper.GetString(args, "type") ?? "sales_summary";
            var dateFromStr = ArgHelper.GetString(args, "date_from");
            var dateToStr = ArgHelper.GetString(args, "date_to");
            var limit = Math.Min(ArgHelper.GetInt(args, "limit", 10), 50);

            DateTime dateTo = DateTime.UtcNow;
            DateTime dateFrom = dateTo.AddDays(-30);

            if (!string.IsNullOrEmpty(dateFromStr) && DateTime.TryParse(dateFromStr, out var df))
                dateFrom = df;
            if (!string.IsNullOrEmpty(dateToStr) && DateTime.TryParse(dateToStr, out var dt))
                dateTo = dt;

            return type switch
            {
                "sales_summary" => await reportsService.GetSalesSummaryAsync(dateFrom, dateTo).WaitAsync(ct),
                "top_products" => await reportsService.GetTopProductsAsync(dateFrom, dateTo, limit).WaitAsync(ct),
                "top_customers" => await reportsService.GetTopCustomersAsync(dateFrom, dateTo, limit).WaitAsync(ct),
                "revenue_by_day" => await reportsService.GetRevenueByDayAsync(dateFrom, dateTo).WaitAsync(ct),
                _ => new { error = $"Loại báo cáo '{type}' không hỗ trợ" }
            };
        }

        private async Task<object> ExecuteGetInventoryStatusAsync(
            Dictionary<string, JsonElement>? args, IServiceProvider sp, CancellationToken ct)
        {
            var statisticsService = sp.GetRequiredService<StatisticsService>();
            var productService = sp.GetRequiredService<ProductService>();

            var threshold = ArgHelper.GetInt(args, "threshold", 10);
            var categoryId = ArgHelper.GetNullableInt(args, "category_id");

            var lowStock = await statisticsService.GetLowStockProductsAsync(threshold).WaitAsync(ct);
            var allProducts = await productService.GetPaginatedProductsAsync(
                1, 1000, null, categoryId, null, null, null, "", null
            ).WaitAsync(ct);

            var totalValue = allProducts.Items
                .Where(p => p.Inventory != null)
                .Sum(p => (p.Inventory?.Quantity ?? 0) * p.Price);

            var outOfStock = allProducts.Items.Count(p => p.Inventory == null || p.Inventory.Quantity <= 0);

            return new
            {
                summary = new
                {
                    totalProducts = allProducts.TotalItems,
                    outOfStockCount = outOfStock,
                    lowStockCount = lowStock.Count(),
                    totalInventoryValue = totalValue
                },
                lowStockProducts = lowStock.Take(20)
            };
        }

        #endregion
    }

    #region Argument Helper

    /// <summary>
    /// Helper class để parse arguments từ JSON - tách riêng để dễ test
    /// </summary>
    public static class ArgHelper
    {
        public static int? GetNullableInt(Dictionary<string, JsonElement>? args, string key)
        {
            if (args != null && args.TryGetValue(key, out var val))
            {
                if (val.ValueKind == JsonValueKind.Number) return val.GetInt32();
                if (val.ValueKind == JsonValueKind.String && int.TryParse(val.GetString(), out int i)) return i;
            }
            return null;
        }

        public static decimal? GetNullableDecimal(Dictionary<string, JsonElement>? args, string key)
        {
            if (args != null && args.TryGetValue(key, out var val))
            {
                if (val.ValueKind == JsonValueKind.Number) return val.GetDecimal();
                if (val.ValueKind == JsonValueKind.String && decimal.TryParse(val.GetString(), out decimal d)) return d;
            }
            return null;
        }

        public static bool? GetNullableBool(Dictionary<string, JsonElement>? args, string key)
        {
            if (args != null && args.TryGetValue(key, out var val))
            {
                if (val.ValueKind == JsonValueKind.True) return true;
                if (val.ValueKind == JsonValueKind.False) return false;
                if (val.ValueKind == JsonValueKind.String)
                {
                    var str = val.GetString()?.ToLower();
                    if (str == "true" || str == "1") return true;
                    if (str == "false" || str == "0") return false;
                }
            }
            return null;
        }

        public static int GetInt(Dictionary<string, JsonElement>? args, string key, int defaultValue)
        {
            return GetNullableInt(args, key) ?? defaultValue;
        }

        public static string? GetString(Dictionary<string, JsonElement>? args, string key, string? defaultValue = null)
        {
            if (args != null && args.TryGetValue(key, out var val))
                return val.GetString() ?? defaultValue;
            return defaultValue;
        }
    }

    #endregion
}
