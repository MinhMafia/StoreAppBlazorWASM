using StoreApp.Data;
using StoreApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Repository
{
    public class StatisticsRepository
    {
        private readonly AppDbContext _context;

        public StatisticsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OverviewStatsDTO> GetOverviewStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            var todayRevenue = await _context.Orders
                .Where(o => (o.Status == "paid" || o.Status == "completed") && o.CreatedAt.Date == today)
                .SumAsync(o => o.TotalAmount);

            var yesterdayRevenue = await _context.Orders
                .Where(o => (o.Status == "paid" || o.Status == "completed") && o.CreatedAt.Date == yesterday)
                .SumAsync(o => o.TotalAmount);

            var revenueChange = yesterdayRevenue > 0
                ? ((todayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100
                : 0;

            var todayOrders = await _context.Orders
                .Where(o => o.CreatedAt.Date == today)
                .CountAsync();

            var yesterdayOrders = await _context.Orders
                .Where(o => o.CreatedAt.Date == yesterday)
                .CountAsync();

            var ordersChange = yesterdayOrders > 0
                ? ((decimal)(todayOrders - yesterdayOrders) / yesterdayOrders) * 100
                : 0;

            var todayProductsSold = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Order != null && oi.Order.CreatedAt.Date == today && (oi.Order.Status == "paid" || oi.Order.Status == "completed"))
                .SumAsync(oi => oi.Quantity);

            var avgOrderValue = todayOrders > 0 ? todayRevenue / todayOrders : 0;

            var totalDiscount = await _context.Orders
                .Where(o => o.CreatedAt.Date == today)
                .SumAsync(o => o.Discount);

            var lowStockCount = await _context.Inventory
                .Where(i => i.Quantity < 10)
                .CountAsync();

            var inventoryValue = await _context.Inventory
                .Include(i => i.Product)
                .SumAsync(i => i.Quantity * (i.Product!.Cost ?? 0));

            return new OverviewStatsDTO
            {
                TodayRevenue = todayRevenue,
                RevenueChange = revenueChange,
                TodayOrders = todayOrders,
                OrdersChange = (int)ordersChange,
                TodayProductsSold = todayProductsSold,
                AverageOrderValue = avgOrderValue,
                TotalDiscountApplied = totalDiscount,
                LowStockCount = lowStockCount,
                InventoryValue = inventoryValue
            };
        }

        public async Task<List<RevenueDataPoint>> GetRevenueByPeriodAsync(int days = 7)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            var data = await _context.Orders
                .Where(o => (o.Status == "paid" || o.Status == "completed") && o.CreatedAt.Date >= startDate)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            return data.Select(d => new RevenueDataPoint
            {
                Label = d.Date.ToString("dd/MM"),
                Revenue = d.Revenue,
                OrderCount = d.OrderCount
            }).ToList();
        }

        public async Task<List<ProductSalesDTO>> GetBestSellersAsync(int limit = 10, int days = 7)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            var data = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Order != null && (oi.Order.Status == "paid" || oi.Order.Status == "completed") && oi.Order.CreatedAt >= startDate)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalPrice)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(limit)
                .ToListAsync();

            var productIds = data.Select(d => d.ProductId).ToList();
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            return data.Select(d =>
            {
                var product = products.First(p => p.Id == d.ProductId);
                return new ProductSalesDTO
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    Sku = product.Sku,
                    QuantitySold = d.QuantitySold,
                    Revenue = d.Revenue,
                    ImageUrl = product.ImageUrl,
                    CategoryName = product.Category?.Name
                };
            }).ToList();
        }

        public async Task<List<ProductInventoryDTO>> GetLowStockProductsAsync(int threshold = 10)
        {
            return await _context.Inventory
                .Include(i => i.Product)
                .Where(i => i.Quantity < threshold)
                .OrderBy(i => i.Quantity)
                .Select(i => new ProductInventoryDTO
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product!.ProductName,
                    Sku = i.Product.Sku,
                    Quantity = i.Quantity,
                    Price = i.Product.Price,
                    LastCheckedAt = i.LastCheckedAt ?? i.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<OrderStatsDTO> GetOrderStatsAsync(int days = 7)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            var orders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate)
                .ToListAsync();

            return new OrderStatsDTO
            {
                TotalOrders = orders.Count,
                PendingOrders = orders.Count(o => o.Status == "pending"),
                PaidOrders = orders.Count(o => o.Status == "paid"),
                CompletedOrders = orders.Count(o => o.Status == "completed"),
                CancelledOrders = orders.Count(o => o.Status == "cancelled"),
                TotalRevenue = orders.Where(o => o.Status == "paid" || o.Status == "completed").Sum(o => o.TotalAmount),
                AverageOrderValue = orders.Where(o => o.Status == "paid" || o.Status == "completed").Any()
                    ? orders.Where(o => o.Status == "paid" || o.Status == "completed").Average(o => o.TotalAmount)
                    : 0
            };
        }
    }
}
