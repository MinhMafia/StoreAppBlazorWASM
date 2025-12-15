using StoreApp.Data;
using StoreApp.Shared;
using StoreApp.Shared.DTO;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Repository
{
    public class ReportsRepository
    {
        private readonly AppDbContext _context;

        public ReportsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SalesReportDTO>> GetSalesReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.OrderItems
                .Include(oi => oi.Order)
                    .ThenInclude(o => o!.Customer)
                .Include(oi => oi.Product)
                .Where(oi => oi.Order != null && (oi.Order.Status == "completed" || oi.Order.Status == "paid")) // Lấy orders đã thanh toán hoặc hoàn thành
                .AsQueryable();

            if (fromDate.HasValue)
            {
                // Normalize to start of day
                var startDate = fromDate.Value.Date;
                query = query.Where(oi => oi.Order!.CreatedAt >= startDate);
            }

            if (toDate.HasValue)
            {
                // Normalize to end of day
                var endDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(oi => oi.Order!.CreatedAt <= endDate);
            }

            var results = await query
                .Select(oi => new SalesReportDTO
                {
                    Date = oi.Order!.CreatedAt,
                    OrderNumber = oi.Order.OrderNumber,
                    CustomerName = oi.Order.Customer != null ? oi.Order.Customer.FullName : null,
                    ProductName = oi.Product != null ? oi.Product.ProductName : "",
                    Sku = oi.Product != null ? oi.Product.Sku : null,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice,
                    Discount = oi.Order.Discount,
                    OrderTotal = oi.Order.TotalAmount,
                    Status = oi.Order.Status
                })
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            return results;
        }

        public async Task<List<InventoryReportDTO>> GetInventoryReportAsync()
        {
            var results = await _context.Inventory
                .Include(i => i.Product)
                    .ThenInclude(p => p!.Category)
                .Where(i => i.Product != null)
                .Select(i => new InventoryReportDTO
                {
                    ProductName = i.Product!.ProductName,
                    Sku = i.Product.Sku,
                    CategoryName = i.Product.Category != null ? i.Product.Category.Name : null,
                    Quantity = i.Quantity,
                    UnitCost = i.Product.Cost ?? 0,
                    TotalValue = i.Quantity * (i.Product.Cost ?? 0),
                    UnitPrice = i.Product.Price,
                    LastUpdated = i.UpdatedAt
                })
                .OrderByDescending(i => i.TotalValue)
                .ToListAsync();

            return results;
        }

        public async Task<SalesSummaryDTO> GetSalesSummaryAsync(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Orders.AsQueryable();

            if (fromDate.HasValue)
            {
                // Normalize to start of day
                var startDate = fromDate.Value.Date;
                query = query.Where(o => o.CreatedAt >= startDate);
            }

            if (toDate.HasValue)
            {
                // Normalize to end of day
                var endDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.CreatedAt <= endDate);
            }

            // Tính các orders đã thanh toán hoặc hoàn thành (paid hoặc completed)
            var paidOrders = query.Where(o => o.Status == "completed" || o.Status == "paid");

            var netRevenue = await paidOrders.SumAsync(o => o.TotalAmount);
            // TotalDiscount: Tính tổng discount của các orders đã thanh toán (để khớp với NetRevenue)
            var totalDiscount = await paidOrders.SumAsync(o => o.Discount);
            var totalOrders = await paidOrders.CountAsync();

            // ProductsSold: Tính sản phẩm từ orders đã thanh toán hoặc hoàn thành
            var productsSoldQuery = _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Order != null && (oi.Order.Status == "completed" || oi.Order.Status == "paid"));

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;
                productsSoldQuery = productsSoldQuery.Where(oi => oi.Order!.CreatedAt >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                productsSoldQuery = productsSoldQuery.Where(oi => oi.Order!.CreatedAt <= endDate);
            }

            var productsSold = await productsSoldQuery.SumAsync(oi => oi.Quantity);

            return new SalesSummaryDTO
            {
                NetRevenue = netRevenue,
                TotalDiscount = totalDiscount,
                TotalOrders = totalOrders,
                ProductsSold = (int)productsSold
            };
        }

        public async Task<List<RevenueByDayDTO>> GetRevenueByDayAsync(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Orders
                .Where(o => o.Status == "completed" || o.Status == "paid")
                .AsQueryable();

            if (fromDate.HasValue)
            {
                // Normalize to start of day
                var startDate = fromDate.Value.Date;
                query = query.Where(o => o.CreatedAt >= startDate);
            }

            if (toDate.HasValue)
            {
                // Normalize to end of day
                var endDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.CreatedAt <= endDate);
            }

            var results = await query
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new RevenueByDayDTO
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(r => r.Date)
                .ToListAsync();

            return results;
        }

        public async Task<List<HighValueInventoryDTO>> GetHighValueInventoryAsync(int limit = 100)
        {
            var results = await _context.Inventory
                .Include(i => i.Product)
                .Where(i => i.Product != null && i.Quantity > 0) 
                .Select(i => new HighValueInventoryDTO
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product!.ProductName,
                    Sku = i.Product.Sku,
                    Quantity = i.Quantity,
                    // Dùng Cost nếu có, nếu không thì dùng Price, nếu cả 2 đều 0 thì dùng 0
                    TotalValue = i.Quantity * (i.Product.Cost.HasValue && i.Product.Cost.Value > 0 
                        ? i.Product.Cost.Value 
                        : (i.Product.Price > 0 ? i.Product.Price : 0))
                })
                .OrderByDescending(i => i.Quantity) // Sắp xếp theo số lượng
                .ThenByDescending(i => i.TotalValue) // Sắp xếp theo giá trị nếu số lượng bằng nhau
                .Take(limit)
                .ToListAsync();

            return results;
        }

        public async Task<PeriodComparisonDTO?> GetPeriodComparisonAsync(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                return null; // Trả về null thay vì throw exception
            }

            var periodDays = (toDate.Value.Date - fromDate.Value.Date).Days + 1;
            var previousFromDate = fromDate.Value.Date.AddDays(-periodDays);
            var previousToDate = fromDate.Value.Date.AddDays(-1);

            var currentPeriod = await GetSalesSummaryAsync(fromDate, toDate);
            var previousPeriod = await GetSalesSummaryAsync(previousFromDate, previousToDate);

            var revenueChange = previousPeriod.NetRevenue > 0
                ? ((currentPeriod.NetRevenue - previousPeriod.NetRevenue) / previousPeriod.NetRevenue) * 100
                : (currentPeriod.NetRevenue > 0 ? 100 : 0);

            var ordersChange = previousPeriod.TotalOrders > 0
                ? ((decimal)(currentPeriod.TotalOrders - previousPeriod.TotalOrders) / previousPeriod.TotalOrders) * 100
                : (currentPeriod.TotalOrders > 0 ? 100 : 0);

            var productsSoldChange = previousPeriod.ProductsSold > 0
                ? ((decimal)(currentPeriod.ProductsSold - previousPeriod.ProductsSold) / previousPeriod.ProductsSold) * 100
                : (currentPeriod.ProductsSold > 0 ? 100 : 0);

            return new PeriodComparisonDTO
            {
                CurrentPeriod = currentPeriod,
                PreviousPeriod = previousPeriod,
                RevenueChangePercent = revenueChange,
                OrdersChangePercent = ordersChange,
                ProductsSoldChangePercent = productsSoldChange
            };
        }

        public async Task<List<TopProductReportDTO>> GetTopProductsAsync(DateTime? fromDate, DateTime? toDate, int limit = 10)
        {
            var query = _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Order != null && (oi.Order.Status == "completed" || oi.Order.Status == "paid"))
                .AsQueryable();

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;
                query = query.Where(oi => oi.Order!.CreatedAt >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(oi => oi.Order!.CreatedAt <= endDate);
            }

            var groupedData = await query
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(limit)
                .ToListAsync();

            // Get products separately to avoid EF Core GroupBy issue
            var productIds = groupedData.Select(d => d.ProductId).ToList();
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            return groupedData.Select(r =>
            {
                var product = products.FirstOrDefault(p => p.Id == r.ProductId);
                return new TopProductReportDTO
                {
                    ProductId = r.ProductId,
                    ProductName = product?.ProductName ?? "",
                    Sku = product?.Sku,
                    CategoryName = product?.Category?.Name,
                    QuantitySold = r.QuantitySold,
                    Revenue = r.Revenue,
                    AveragePrice = r.QuantitySold > 0 ? r.Revenue / r.QuantitySold : 0
                };
            }).ToList();
        }

        public async Task<List<TopCustomerReportDTO>> GetTopCustomersAsync(DateTime? fromDate, DateTime? toDate, int limit = 10)
        {
            var query = _context.Orders
                .Where(o => o.Status == "completed" || o.Status == "paid")
                .AsQueryable();

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;
                query = query.Where(o => o.CreatedAt >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.CreatedAt <= endDate);
            }

            var groupedData = await query
                .Where(o => o.CustomerId.HasValue)
                .GroupBy(o => o.CustomerId!.Value)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(limit)
                .ToListAsync();

            // Get customers separately to avoid EF Core GroupBy issue
            var customerIds = groupedData.Select(d => d.CustomerId).ToList();
            var customers = await _context.Customers
                .Where(c => customerIds.Contains(c.Id))
                .ToListAsync();

            return groupedData.Select(r =>
            {
                var customer = customers.FirstOrDefault(c => c.Id == r.CustomerId);
                return new TopCustomerReportDTO
                {
                    CustomerId = r.CustomerId,
                    CustomerName = customer?.FullName ?? "",
                    Email = customer?.Email,
                    Phone = customer?.Phone,
                    OrderCount = r.OrderCount,
                    TotalSpent = r.TotalSpent,
                    AverageOrderValue = r.OrderCount > 0 ? r.TotalSpent / r.OrderCount : 0
                };
            }).ToList();
        }

        public async Task<List<SalesByStaffDTO>> GetSalesByStaffAsync(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Orders
                .Include(o => o.Staff)
                .Where(o => o.Status == "completed" && o.StaffId.HasValue)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;
                query = query.Where(o => o.CreatedAt >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.CreatedAt <= endDate);
            }

            var results = await query
                .GroupBy(o => o.StaffId!.Value)
                .Select(g => new SalesByStaffDTO
                {
                    UserId = g.Key,
                    UserName = g.First().Staff!.Username,
                    FullName = g.First().Staff.FullName,
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(o => o.TotalAmount),
                    AverageOrderValue = g.Count() > 0 ? g.Average(o => o.TotalAmount) : 0
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

            return results;
        }
    }
}

