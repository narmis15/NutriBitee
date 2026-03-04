using Microsoft.AspNetCore.Mvc;
using NUTRIBITE.Models.Reports;
using NutriBite.Filters;
using NUTRIBITE.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace NUTRIBITE.Controllers
{
    [AdminAuthorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Main entry for Reports & Analytics
        [HttpGet]
        public IActionResult Dashboard()
        {
            return View();
        }

        // Focused report pages - views live in Views/Reports
        [HttpGet]
        public IActionResult OrderReports()
        {
            return View(new OrderReportModel());
        }

        [HttpGet]
        public IActionResult SalesAnalytics()
        {
            // return view; the view will request JSON data to render cards/chart/table
            return View(new NUTRIBITE.Models.Reports.SalesReportModel());
        }

        [HttpGet]
        public IActionResult VendorPerformance()
        {
            return View(new VendorPerformanceModel());
        }

        [HttpGet]
        public IActionResult LocationAnalytics()
        {
            // returns the view located at Views/Reports/LocationAnalytics.cshtml
            return View();
        }

        [HttpGet]
        public IActionResult PaymentReports()
        {
            // View will request JSON data; provide an (empty) model for initial load
            return View(new NUTRIBITE.Models.Reports.PaymentReportModel());
        }

        [AdminAuthorize]
        [HttpGet]
        public IActionResult SystemLogs()
        {
            // Returns the view that hosts the log viewer UI
            return View();
        }

        // JSON endpoint used by the Dashboard view to populate cards, trend and alerts.
        [HttpGet]
        public IActionResult GetDashboardData()
        {
            // Use real DB data while keeping the same JSON shape expected by the UI.
            var today = DateTime.Today;
            var todayStart = today;
            var tomorrowStart = today.AddDays(1);
            var start14 = todayStart.AddDays(-13);

            // 1) Summary cards
            int todaysOrders = _context.OrderTables
                .Count(o => o.CreatedAt >= todayStart && o.CreatedAt < tomorrowStart);

            decimal todaysRevenue = _context.Payments
                .Where(p => p.CreatedAt >= todayStart && p.CreatedAt < tomorrowStart)
                .Select(p => p.Amount ?? 0m)
                .Sum();

            // Top vendor heuristic: vendor with most food items listed (safe, non-invasive)
            var topVendor = _context.VendorSignups
                .Select(v => new { v.VendorName, Count = _context.Foods.Count(f => f.VendorId == v.VendorId) })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault()?.VendorName ?? "—";

            // Top location heuristic: most frequent PickupSlot (fallback to "—")
            var topLocation = _context.OrderTables
                .Where(o => !string.IsNullOrEmpty(o.PickupSlot))
                .GroupBy(o => o.PickupSlot)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "—";

            var summary = new[]
            {
                new SummaryCard { Title = "Today's Orders", Value = todaysOrders.ToString(), SubText = "New orders today", Icon = "🧾" },
                new SummaryCard { Title = "Today's Revenue", Value = "₹" + todaysRevenue.ToString("N2"), SubText = "Gross sales today", Icon = "💰" },
                new SummaryCard { Title = "Top Vendor", Value = topVendor, SubText = "Most listed items", Icon = "⭐" },
                new SummaryCard { Title = "Top Location", Value = topLocation, SubText = "Most pickups", Icon = "📍" }
            };

            // 2) Trend (last 14 days)
            var trendList = new List<TrendPoint>();
            for (int i = 0; i < 14; i++)
            {
                var d = start14.AddDays(i);
                var dNext = d.AddDays(1);

                int ordersCount = _context.OrderTables
                    .Count(o => o.CreatedAt >= d && o.CreatedAt < dNext);

                decimal revenue = _context.Payments
                    .Where(p => p.CreatedAt >= d && p.CreatedAt < dNext)
                    .Select(p => p.Amount ?? 0m)
                    .Sum();

                trendList.Add(new TrendPoint
                {
                    Label = d.ToString("MM-dd"),
                    Orders = ordersCount,
                    Revenue = revenue
                });
            }

            // 3) Alerts: recent flagged orders and recent refunds (combine)
            var flaggedAlerts = _context.OrderTables
                .Where(o => o.IsFlagged == true)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new AlertModel
                {
                    OrderId = o.OrderId,
                    Type = "Flagged",
                    Message = string.IsNullOrEmpty(o.FlagReason) ? "Order flagged for review" : o.FlagReason,
                    Time = o.CreatedAt ?? DateTime.Now,
                    Severity = "medium"
                })
                .ToList();

            var refundAlerts = _context.Payments
                .Where(p => (p.IsRefunded == true) || (!string.IsNullOrEmpty(p.RefundStatus) && p.RefundStatus.ToLower().Contains("refund")))
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Take(5)
                .Select(p => new AlertModel
                {
                    OrderId = p.OrderId ?? 0,
                    Type = "Refund",
                    Message = string.IsNullOrEmpty(p.RefundStatus) ? "Refund processed" : p.RefundStatus,
                    Time = p.UpdatedAt ?? p.CreatedAt ?? DateTime.Now,
                    Severity = "high"
                })
                .ToList();

            var alerts = flaggedAlerts
                .Concat(refundAlerts)
                .OrderByDescending(a => a.Time)
                .Take(6)
                .ToArray();

            var model = new DashboardModel
            {
                Summary = summary,
                Trend = trendList.ToArray(),
                Alerts = alerts
            };

            return Json(model);
        }

        // JSON endpoint backing the OrderReports page (mocked data - replace with DB queries)
        [HttpGet]
        public IActionResult GetOrderReportsData(DateTime? from, DateTime? to, string status)
        {
            DateTime start = from?.Date ?? DateTime.Today.AddDays(-13);
            DateTime end = to?.Date ?? DateTime.Today;
            if (start > end) (start, end) = (end, start);
            DateTime endExclusive = end.AddDays(1);

            var query = _context.OrderTables
                .Include(o => o.OrderItems)
                .Include(o => o.Payments)
                .Where(o => o.CreatedAt >= start && o.CreatedAt < endExclusive);

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = query
                .AsEnumerable() // switch to in-memory for composed projection using navigation collections
                .Select(o => new
                {
                    OrderId = o.OrderId,
                    OrderDate = o.CreatedAt,
                    CustomerName = o.CustomerName ?? "",
                    ItemsCount = o.TotalItems ?? o.OrderItems?.Count ?? 0,
                    PickupSlot = o.PickupSlot ?? "",
                    Amount = (o.Payments?.Sum(p => p.Amount) ?? 0m),
                    TotalCalories = o.TotalCalories ?? 0,
                    PaymentStatus = o.PaymentStatus ?? "",
                    Status = o.Status ?? ""
                })
                .OrderByDescending(x => x.OrderDate)
                .ToList();

            // Summary
            var total = orders.Count;
            var completed = orders.Count(o => string.Equals(o.Status, "Picked", StringComparison.OrdinalIgnoreCase));
            var pending = orders.Count(o => new[] { "New", "Accepted", "Ready for Pickup" }.Contains((o.Status ?? ""), StringComparer.OrdinalIgnoreCase));
            var cancelled = orders.Count(o => string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));

            var summary = new[]
            {
                new SummaryCard { Title = "Total Orders", Value = total.ToString(), SubText = $"{start:yyyy-MM-dd} → {end:yyyy-MM-dd}" },
                new SummaryCard { Title = "Completed", Value = completed.ToString(), SubText = "Picked / Completed" },
                new SummaryCard { Title = "Pending", Value = pending.ToString(), SubText = "New / Accepted / Ready" },
                new SummaryCard { Title = "Cancelled", Value = cancelled.ToString(), SubText = "Cancelled orders" }
            };

            // Trend: orders per day
            var trend = Enumerable.Range(0, (end - start).Days + 1)
                .Select(i =>
                {
                    var d = start.AddDays(i);
                    var next = d.AddDays(1);
                    var cnt = _context.OrderTables.Count(o => o.CreatedAt >= d && o.CreatedAt < next);
                    return new TrendPoint { Label = d.ToString("MM-dd"), Orders = cnt };
                }).ToArray();

            return Json(new { summary, orders, trend });
        }

        [HttpGet]
        public IActionResult GetSalesAnalyticsData(DateTime? from, DateTime? to, string period = "daily")
        {
            DateTime end = to?.Date ?? DateTime.Today;
            DateTime start = from?.Date ?? end.AddDays(-13);
            if (start > end) (start, end) = (end, start);
            DateTime endExclusive = end.AddDays(1);

            // Payments in range
            var paymentsInRange = _context.Payments
                .Where(p => p.CreatedAt >= start && p.CreatedAt < endExclusive);

            decimal totalRevenue = paymentsInRange.Select(p => p.Amount ?? 0m).Sum();
            int totalOrders = _context.OrderTables.Count(o => o.CreatedAt >= start && o.CreatedAt < endExclusive);
            decimal avgOrder = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 2) : 0m;
            decimal profit = Math.Round(totalRevenue * 0.12m, 2);

            // Trend and breakdown
            var breakdown = new List<NUTRIBITE.Models.Reports.BreakdownRow>();
            var trend = new List<NUTRIBITE.Models.Reports.TrendPoint>();

            if (period?.ToLowerInvariant() == "monthly")
            {
                var monthly = paymentsInRange
                    .Where(p => p.CreatedAt.HasValue)
                    .GroupBy(p => new { p.CreatedAt.Value.Year, p.CreatedAt.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Revenue = g.Sum(x => x.Amount ?? 0m),
                        Orders = _context.OrderTables.Count(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Year == g.Key.Year && o.CreatedAt.Value.Month == g.Key.Month)
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToList();

                foreach (var m in monthly)
                {
                    var label = $"{m.Year}-{m.Month:00}";
                    trend.Add(new NUTRIBITE.Models.Reports.TrendPoint { Label = label, Revenue = m.Revenue });
                    breakdown.Add(new NUTRIBITE.Models.Reports.BreakdownRow
                    {
                        PeriodLabel = label,
                        Orders = m.Orders,
                        Revenue = m.Revenue,
                        AvgOrderValue = m.Orders > 0 ? Math.Round(m.Revenue / m.Orders, 2) : 0m,
                        Profit = Math.Round(m.Revenue * 0.12m, 2)
                    });
                }
            }
            else
            {
                // daily
                for (DateTime d = start; d <= end; d = d.AddDays(1))
                {
                    var next = d.AddDays(1);
                    var rev = paymentsInRange.Where(p => p.CreatedAt >= d && p.CreatedAt < next).Select(p => p.Amount ?? 0m).Sum();
                    var ord = _context.OrderTables.Count(o => o.CreatedAt >= d && o.CreatedAt < next);

                    trend.Add(new NUTRIBITE.Models.Reports.TrendPoint { Label = d.ToString("MM-dd"), Revenue = rev });
                    breakdown.Add(new NUTRIBITE.Models.Reports.BreakdownRow
                    {
                        PeriodLabel = d.ToString("yyyy-MM-dd"),
                        Orders = ord,
                        Revenue = rev,
                        AvgOrderValue = ord > 0 ? Math.Round(rev / ord, 2) : 0m,
                        Profit = Math.Round(rev * 0.12m, 2)
                    });
                }
            }

            var model = new NUTRIBITE.Models.Reports.SalesReportModel
            {
                TotalRevenue = Math.Round(totalRevenue, 2),
                AverageOrderValue = Math.Round(avgOrder, 2),
                Profit = profit,
                Trend = trend.ToArray(),
                Breakdown = breakdown.ToArray()
            };

            return Json(model);
        }

        [HttpGet]
        public IActionResult GetVendorPerformanceData(DateTime? from, DateTime? to, int top = 10)
        {
            DateTime end = to?.Date ?? DateTime.Today;
            DateTime start = from?.Date ?? end.AddDays(-29);
            if (start > end) (start, end) = (end, start);
            DateTime endExclusive = end.AddDays(1);

            // For each vendor, attempt to compute orders & revenue by matching OrderItems.ItemName to Foods.Name.
            var vendors = _context.VendorSignups.ToList();

            var vendorRows = new List<Models.Reports.VendorRow>();
            foreach (var v in vendors)
            {
                var foodNames = _context.Foods
                    .Where(f => f.VendorId == v.VendorId && !string.IsNullOrEmpty(f.Name))
                    .Select(f => f.Name)
                    .ToList();

                if (foodNames.Count == 0)
                {
                    vendorRows.Add(new Models.Reports.VendorRow
                    {
                        VendorId = v.VendorId,
                        VendorName = v.VendorName,
                        Orders = 0,
                        Revenue = 0m,
                        CancellationRate = 0m,
                        Performance = "No Data"
                    });
                    continue;
                }

                var orderIds = _context.OrderItems
                    .Where(oi => foodNames.Contains(oi.ItemName))
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .ToList();

                var ordersCount = _context.OrderTables
                    .Count(o => orderIds.Contains(o.OrderId) && o.CreatedAt >= start && o.CreatedAt < endExclusive);

                var revenue = _context.Payments
                    .Where(p => p.CreatedAt >= start && p.CreatedAt < endExclusive && p.OrderId != null && orderIds.Contains(p.OrderId.Value))
                    .Select(p => p.Amount ?? 0m)
                    .Sum();

                var cancelled = _context.OrderTables
                    .Count(o => orderIds.Contains(o.OrderId) && o.Status == "Cancelled");

                decimal cancelRate = (ordersCount > 0) ? Math.Round((decimal)cancelled / ordersCount * 100m, 2) : 0m;

                vendorRows.Add(new Models.Reports.VendorRow
                {
                    VendorId = v.VendorId,
                    VendorName = v.VendorName,
                    Orders = ordersCount,
                    Revenue = Math.Round(revenue, 2),
                    CancellationRate = cancelRate,
                    Performance = (revenue > 50000 && cancelRate < 4) ? "Good" : (cancelRate > 8 || revenue < 8000 ? "Poor" : "Average")
                });
            }

            var ranked = vendorRows.OrderByDescending(v => v.Revenue).ToArray();
            var topVendors = ranked.Take(top).ToArray();

            var chartLabels = topVendors.Select(v => v.VendorName).ToArray();
            var chartValues = topVendors.Select(v => v.Revenue).ToArray();

            var summary = new Models.Reports.VendorSummary
            {
                TotalVendors = vendorRows.Count,
                ActiveVendors = vendorRows.Count(v => v.Orders > 0),
                TotalRevenue = Math.Round(vendorRows.Sum(v => v.Revenue), 2),
                TotalOrders = vendorRows.Sum(v => v.Orders)
            };

            var model = new Models.Reports.VendorPerformanceModel
            {
                Summary = summary,
                Vendors = topVendors,
                Chart = new Models.Reports.ChartData
                {
                    Labels = chartLabels,
                    Values = chartValues
                }
            };

            return Json(model);
        }

        [AdminAuthorize]
        [HttpGet]
        public IActionResult GetLocationAnalyticsData(string city = "All")
        {
            // The database doesn't contain explicit city fields on OrderTable in this schema.
            // We'll use PickupSlot grouping as a practical proxy for "location/time" demand.
            DateTime start = DateTime.Today.AddDays(-29);
            DateTime end = DateTime.Today.AddDays(1);

            var grouped = _context.OrderTables
                .Where(o => !string.IsNullOrEmpty(o.PickupSlot) && o.CreatedAt >= start && o.CreatedAt < end)
                .GroupBy(o => o.PickupSlot)
                .Select(g => new
                {
                    PickupSlot = g.Key,
                    OrdersCount = g.Count(),
                    Revenue = _context.Payments.Where(p => p.OrderId != null && g.Select(x => x.OrderId).Contains(p.OrderId.Value)).Select(p => p.Amount ?? 0m).Sum()
                })
                .OrderByDescending(x => x.OrdersCount)
                .ToList();

            var locations = grouped.Select(g => new Models.Reports.LocationDemandModel
            {
                City = g.PickupSlot ?? "Unknown",
                Region = "",
                OrdersCount = g.OrdersCount,
                Percentage = 0m
            }).ToList();

            var total = locations.Sum(x => x.OrdersCount);
            if (total > 0)
            {
                foreach (var loc in locations)
                    loc.Percentage = Math.Round((decimal)loc.OrdersCount / total * 100m, 2);
            }

            var chart = locations.Select(l => new Models.Reports.ChartPoint
            {
                Label = l.City + (string.IsNullOrEmpty(l.Region) ? "" : " - " + l.Region),
                Value = l.OrdersCount
            }).ToArray();

            var cities = locations.Select(l => l.City).Distinct().ToArray();

            var result = new Models.Reports.LocationAnalyticsModel
            {
                SelectedCity = city ?? "All",
                Cities = cities,
                Locations = locations.ToArray(),
                Chart = chart
            };

            return Json(result);
        }

        [HttpGet]
        public IActionResult GetPaymentReportsData(DateTime? from, DateTime? to, string status = "All")
        {
            DateTime end = to?.Date ?? DateTime.Today;
            DateTime start = from?.Date ?? end.AddDays(-13);
            if (start > end) (start, end) = (end, start);
            DateTime endExclusive = end.AddDays(1);

            var payments = _context.Payments
                .Where(p => p.CreatedAt >= start && p.CreatedAt < endExclusive)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    PaymentId = p.Id,
                    OrderId = p.OrderId,
                    PaymentDate = p.CreatedAt,
                    CustomerName = _context.OrderTables.Where(o => o.OrderId == p.OrderId).Select(o => o.CustomerName).FirstOrDefault() ?? "",
                    Method = p.PaymentMode ?? "",
                    Amount = p.Amount ?? 0m,
                    Status = p.IsRefunded == true ? "Refunded" : (!string.IsNullOrEmpty(p.RefundStatus) ? p.RefundStatus : "Success"),
                    GatewayRef = "", // not available in current schema
                    Notes = p.RefundStatus ?? ""
                })
                .ToList();

            if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                payments = payments.Where(p => string.Equals(p.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var totalSuccess = payments.Count(p => string.Equals(p.Status, "Success", StringComparison.OrdinalIgnoreCase));
            var totalFailed = payments.Count(p => string.Equals(p.Status, "Failed", StringComparison.OrdinalIgnoreCase));
            var totalRefunded = payments.Count(p => string.Equals(p.Status, "Refunded", StringComparison.OrdinalIgnoreCase));
            var totalCancelled = payments.Count(p => string.Equals(p.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));

            var model = new NUTRIBITE.Models.Reports.PaymentReportModel
            {
                Payments = payments.Select(p => new NUTRIBITE.Models.Reports.PaymentRow
                {
                    PaymentId = p.PaymentId,
                    OrderId = p.OrderId ?? 0,
                    PaymentDate = p.PaymentDate ?? DateTime.MinValue,
                    CustomerName = p.CustomerName,
                    Method = p.Method,
                    Amount = p.Amount,
                    Status = p.Status,
                    GatewayRef = p.GatewayRef,
                    Notes = p.Notes
                }).ToArray(),
                TotalSuccess = totalSuccess,
                TotalFailed = totalFailed,
                TotalRefunded = totalRefunded,
                TotalCancelled = totalCancelled
            };

            return Json(model);
        }

        [AdminAuthorize]
        [HttpGet]
        public IActionResult GetSystemLogs(
            string severity = "All",
            DateTime? from = null,
            DateTime? to = null,
            string? q = null,
            int page = 1,
            int pageSize = 50)
        {
            // NOTE: sample in-memory data. Replace with real log storage (database, Seq, Elastic, files) queries.
            DateTime end = (to ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);
            DateTime start = from?.Date ?? DateTime.Now.Date.AddDays(-7);

            var rnd = new Random(123);
            var levels = new[] { "Info", "Warning", "Error", "Debug" };
            var sources = new[] { "API", "Worker", "Scheduler", "Auth", "PaymentGateway" };

            List<NUTRIBITE.Models.Reports.SystemLogModel> logs = new();
            for (int i = 0; i < 500; i++)
            {
                var ts = DateTime.Now.AddMinutes(-i * 5);
                logs.Add(new NUTRIBITE.Models.Reports.SystemLogModel
                {
                    Timestamp = ts,
                    Level = levels[rnd.Next(levels.Length)],
                    Source = sources[rnd.Next(sources.Length)],
                    Message = $"Sample log message #{i} - {(i % 5 == 0 ? "critical path" : "normal op")}",
                    Details = $"Stack/Details for log #{i}.",
                    User = i % 3 == 0 ? "system" : $"user{rnd.Next(1,50)}"
                });
            }

            // Apply filters
            var query = logs.AsQueryable();

            if (!string.IsNullOrEmpty(severity) && !severity.Equals("All", StringComparison.OrdinalIgnoreCase))
                query = query.Where(l => l.Level.Equals(severity, StringComparison.OrdinalIgnoreCase));

            query = query.Where(l => l.Timestamp >= start && l.Timestamp <= end);

            if (!string.IsNullOrWhiteSpace(q))
            {
                string ql = q.Trim();
                query = query.Where(l =>
                    l.Message.Contains(ql, StringComparison.OrdinalIgnoreCase) ||
                    l.Details.Contains(ql, StringComparison.OrdinalIgnoreCase) ||
                    l.Source.Contains(ql, StringComparison.OrdinalIgnoreCase) ||
                    l.User.Contains(ql, StringComparison.OrdinalIgnoreCase));
            }

            int total = query.Count();

            var pageItems = query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return Json(new
            {
                totalCount = total,
                page,
                pageSize,
                items = pageItems.Select(l => new
                {
                    timestamp = l.Timestamp,
                    level = l.Level,
                    source = l.Source,
                    message = l.Message,
                    details = l.Details,
                    user = l.User
                })
            });
        }
    }
}
