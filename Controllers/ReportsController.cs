using Microsoft.AspNetCore.Mvc;
using NUTRIBITE.Models.Reports;
using NutriBite.Filters;

namespace NUTRIBITE.Controllers
{
    [AdminAuthorize]
    public class ReportsController : Controller
    {
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
            // TODO: replace these sample values with real queries against your DB.
            var now = DateTime.Today;
            var trend = Enumerable.Range(0, 14)
                .Select(i => new TrendPoint
                {
                    Label = now.AddDays(-13 + i).ToString("MM-dd"),
                    Orders = 50 + (i * 3) + (i % 3 == 0 ? 10 : 0),
                    Revenue = 1200 + (i * 45)
                }).ToArray();

            var model = new DashboardModel
            {
                Summary = new[]
                {
                    new SummaryCard { Title = "Today's Orders", Value = "124", SubText = "New orders today", Icon = "🧾" },
                    new SummaryCard { Title = "Today's Revenue", Value = "₹15,340.50", SubText = "Gross sales today", Icon = "💰" },
                    new SummaryCard { Title = "Top Vendor", Value = "NutriBite Central", SubText = "Most sales today", Icon = "⭐" },
                    new SummaryCard { Title = "Top Location", Value = "Downtown", SubText = "Highest demand", Icon = "📍" }
                },
                Trend = trend,
                Alerts = new[]
                {
                    new AlertModel { OrderId = 1051, Type = "FailedPayment", Message = "Payment failed (card decline)", Time = DateTime.Now.AddMinutes(-18), Severity = "high" },
                    new AlertModel { OrderId = 1073, Type = "Flagged", Message = "Suspicious duplicate order", Time = DateTime.Now.AddHours(-1), Severity = "medium" },
                    new AlertModel { OrderId = 1032, Type = "FailedPayment", Message = "Payment gateway timeout", Time = DateTime.Now.AddHours(-3), Severity = "low" }
                }
            };

            return Json(model);
        }

        // JSON endpoint backing the OrderReports page (mocked data - replace with DB queries)
        [HttpGet]
        public IActionResult GetOrderReportsData(DateTime? from, DateTime? to, string status)
        {
            DateTime start = from?.Date ?? DateTime.Today.AddDays(-13);
            DateTime end = to?.Date ?? DateTime.Today;
            // normalize
            if (start > end) (start, end) = (end, start);

            // sample statuses used in table
            var possibleStatuses = new[] { "New", "Accepted", "Ready for Pickup", "Picked", "Cancelled" };

            var rnd = new Random(123);
            var orders = new List<OrderRow>();
            int id = 1000;
            // create mocked orders within date range
            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                int dayCount = 3 + rnd.Next(5); // small sample per day
                for (int i = 0; i < dayCount; i++)
                {
                    var st = possibleStatuses[rnd.Next(possibleStatuses.Length)];
                    if (!string.IsNullOrEmpty(status) && status != "All" && !st.Equals(status, StringComparison.OrdinalIgnoreCase))
                        continue;

                    orders.Add(new OrderRow
                    {
                        OrderId = id++,
                        OrderDate = d.AddHours(8 + rnd.Next(8)),
                        CustomerName = "Customer " + rnd.Next(1, 200),
                        ItemsCount = 1 + rnd.Next(4),
                        PickupSlot = (9 + rnd.Next(8)) + ":00 - " + (10 + rnd.Next(8)) + ":00",
                        Amount = Math.Round((decimal)(100 + rnd.Next(900)), 2),
                        TotalCalories = 200 + rnd.Next(800),
                        PaymentStatus = rnd.Next(10) > 1 ? "Paid" : "Failed",
                        Status = st,
                        IsFlagged = rnd.Next(20) == 0
                    });
                }
            }

            // summary cards
            var total = orders.Count;
            var completed = orders.Count(o => o.Status == "Picked");
            var pending = orders.Count(o => o.Status == "New" || o.Status == "Accepted" || o.Status == "Ready for Pickup");
            var cancelled = orders.Count(o => o.Status == "Cancelled");

            var summary = new[]
            {
                new SummaryCard { Title = "Total Orders", Value = total.ToString(), SubText = $"{start:yyyy-MM-dd} → {end:yyyy-MM-dd}" },
                new SummaryCard { Title = "Completed", Value = completed.ToString(), SubText = "Picked / Completed" },
                new SummaryCard { Title = "Pending", Value = pending.ToString(), SubText = "New / Accepted / Ready" },
                new SummaryCard { Title = "Cancelled", Value = cancelled.ToString(), SubText = "Cancelled orders" }
            };

            // trend by day
            var trend = new List<TrendPoint>();
            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                trend.Add(new TrendPoint
                {
                    Label = d.ToString("MM-dd"),
                    Orders = orders.Count(o => o.OrderDate.Date == d.Date)
                });
            }

            // return paged-ish sample (client can implement paging)
            var result = new
            {
                Summary = summary,
                Orders = orders.OrderByDescending(o => o.OrderDate).ToArray(),
                Trend = trend.ToArray()
            };

            return Json(result);
        }

        /// <summary>
        /// JSON endpoint used by SalesAnalytics view. Currently returns sample data.
        /// Replace with real DB queries and aggregation logic.
        /// </summary>
        [HttpGet]
        public IActionResult GetSalesAnalyticsData(DateTime? from, DateTime? to, string period = "daily")
        {
            DateTime end = to?.Date ?? DateTime.Today;
            DateTime start = from?.Date ?? end.AddDays(-13); // default last 14 days
            if (start > end) (start, end) = (end, start);

            // sample generation (replace with your SQL/EF)
            var rnd = new Random(42);
            var days = (end - start).Days + 1;
            var trend = new List<NUTRIBITE.Models.Reports.TrendPoint>();
            var breakdown = new List<NUTRIBITE.Models.Reports.BreakdownRow>();

            decimal totalRev = 0m;
            int totalOrders = 0;

            for (int i = 0; i < days; i++)
            {
                var d = start.AddDays(i);
                var orders = 40 + rnd.Next(60);
                var revenue = Math.Round((decimal)(orders * (100 + rnd.NextDouble() * 200)), 2);
                var avg = orders > 0 ? Math.Round(revenue / orders, 2) : 0m;
                var profit = Math.Round(revenue * 0.12m, 2); // sample 12% margin

                trend.Add(new NUTRIBITE.Models.Reports.TrendPoint { Label = d.ToString("MM-dd"), Revenue = revenue });
                breakdown.Add(new NUTRIBITE.Models.Reports.BreakdownRow
                {
                    PeriodLabel = d.ToString("yyyy-MM-dd"),
                    Orders = orders,
                    Revenue = revenue,
                    AvgOrderValue = avg,
                    Profit = profit
                });

                totalRev += revenue;
                totalOrders += orders;
            }

            var model = new NUTRIBITE.Models.Reports.SalesReportModel
            {
                TotalRevenue = Math.Round(totalRev, 2),
                AverageOrderValue = totalOrders > 0 ? Math.Round(totalRev / totalOrders, 2) : 0m,
                Profit = Math.Round(totalRev * 0.12m, 2),
                Trend = trend.ToArray(),
                Breakdown = breakdown.ToArray()
            };

            return Json(model);
        }

        [HttpGet]
        public IActionResult GetVendorPerformanceData(DateTime? from, DateTime? to, int top = 10)
        {
            // NOTE: This returns sample/mock data. Replace SQL/EF queries to pull real metrics.
            DateTime end = to?.Date ?? DateTime.Today;
            DateTime start = from?.Date ?? end.AddDays(-29); // default last 30 days
            if (start > end) (start, end) = (end, start);

            var rnd = new Random(42);

            // build mock vendor list
            var vendors = new List<Models.Reports.VendorRow>();
            for (int i = 1; i <= 20; i++)
            {
                int orders = rnd.Next(20, 600);
                decimal revenue = Math.Round((decimal)(orders * (80 + rnd.NextDouble() * 250)), 2);
                int cancelled = (int)(orders * (rnd.NextDouble() * 0.12)); // up to 12% cancel
                decimal cancelRate = orders == 0 ? 0m : Math.Round((decimal)cancelled / orders * 100m, 2);

                string perf = "Average";
                if (revenue > 50000 && cancelRate < 4) perf = "Good";
                else if (cancelRate > 8 || revenue < 8000) perf = "Poor";

                vendors.Add(new Models.Reports.VendorRow
                {
                    VendorId = i,
                    VendorName = $"Vendor {i}",
                    Orders = orders,
                    Revenue = revenue,
                    CancellationRate = cancelRate,
                    Performance = perf
                });
            }

            // rank by revenue desc and take top N
            var ranked = vendors.OrderByDescending(v => v.Revenue).ToArray();
            var topVendors = ranked.Take(top).ToArray();

            // chart data - simple revenue bars for top vendors
            var chartLabels = topVendors.Select(v => v.VendorName).ToArray();
            var chartValues = topVendors.Select(v => v.Revenue).ToArray();

            var summary = new Models.Reports.VendorSummary
            {
                TotalVendors = vendors.Count,
                ActiveVendors = vendors.Count(v => v.Orders > 0),
                TotalRevenue = Math.Round(vendors.Sum(v => v.Revenue), 2),
                TotalOrders = vendors.Sum(v => v.Orders)
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
            // NOTE: sample/mock implementation. Replace with real SQL/EF queries against your DB.
            try
            {
                // sample cities and regions
                var cities = new[] { "All", "Downtown", "Uptown", "Suburb", "Airport" };

                // mock location rows (in real code aggregate OrderTable by City/Region)
                var rnd = new Random(42);
                var baseLocations = new[]
                {
                    new { City = "Downtown", Region = "Central" },
                    new { City = "Downtown", Region = "Market" },
                    new { City = "Uptown", Region = "North" },
                    new { City = "Suburb", Region = "West End" },
                    new { City = "Airport", Region = "Terminal" }
                };

                var locations = baseLocations
                    .Where(l => city == "All" || l.City == city)
                    .Select(l => new Models.Reports.LocationDemandModel
                    {
                        City = l.City,
                        Region = l.Region,
                        OrdersCount = 20 + rnd.Next(180),
                        Percentage = 0m // will compute below
                    })
                    .ToList();

                var total = locations.Sum(x => x.OrdersCount);
                if (total > 0)
                {
                    foreach (var loc in locations)
                        loc.Percentage = Math.Round((decimal)loc.OrdersCount / total * 100m, 2);
                }

                var chart = locations.Select(l => new Models.Reports.ChartPoint
                {
                    Label = $"{l.City} - {l.Region}",
                    Value = l.OrdersCount
                }).ToArray();

                var result = new Models.Reports.LocationAnalyticsModel
                {
                    SelectedCity = city ?? "All",
                    Cities = cities,
                    Locations = locations.ToArray(),
                    Chart = chart
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetPaymentReportsData(DateTime? from, DateTime? to, string status = "All")
        {
            // NOTE: mock data — replace with real SQL/EF queries using your DB schema.
            DateTime end = to?.Date ?? DateTime.Today;
            DateTime start = from?.Date ?? end.AddDays(-13);
            if (start > end) (start, end) = (end, start);

            var rnd = new Random(123);
            var list = new List<NUTRIBITE.Models.Reports.PaymentRow>();
            int id = 5000;

            // generate sample payments
            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                int perDay = 8 + rnd.Next(8);
                for (int i = 0; i < perDay; i++)
                {
                    var sts = rnd.NextDouble();
                    string st = sts > 0.85 ? "Failed" : sts > 0.95 ? "Cancelled" : (sts > 0.98 ? "Refunded" : "Success");
                    if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(st, status, StringComparison.OrdinalIgnoreCase))
                        continue;

                    list.Add(new NUTRIBITE.Models.Reports.PaymentRow
                    {
                        PaymentId = id++,
                        OrderId = 1000 + rnd.Next(400),
                        PaymentDate = d.AddHours(8 + rnd.Next(8)).AddMinutes(rnd.Next(60)),
                        CustomerName = "Cust " + rnd.Next(100, 999),
                        Method = rnd.Next(10) > 2 ? "Card" : "UPI",
                        Amount = Math.Round((decimal)(50 + rnd.NextDouble() * 900), 2),
                        Status = st,
                        GatewayRef = "GW" + rnd.Next(100000, 999999),
                        Notes = st == "Failed" ? "Card declined" : ""
                    });
                }
            }

            var model = new NUTRIBITE.Models.Reports.PaymentReportModel
            {
                Payments = list.OrderByDescending(p => p.PaymentDate).ToArray(),
                TotalSuccess = list.Count(p => p.Status == "Success"),
                TotalFailed = list.Count(p => p.Status == "Failed"),
                TotalRefunded = list.Count(p => p.Status == "Refunded"),
                TotalCancelled = list.Count(p => p.Status == "Cancelled")
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
