namespace NUTRIBITE.Models.Reports
{
    public class VendorPerformanceModel
    {
        public VendorSummary Summary { get; set; } = new VendorSummary();
        public VendorRow[] Vendors { get; set; } = Array.Empty<VendorRow>();
        public ChartData Chart { get; set; } = new ChartData();
    }

    public class VendorSummary
    {
        public int TotalVendors { get; set; }
        public int ActiveVendors { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
    }

    public class VendorRow
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; } = "";
        public int Orders { get; set; }
        public decimal Revenue { get; set; }
        public decimal CancellationRate { get; set; } // 0..100
        public string Performance { get; set; } = ""; // Good | Average | Poor
    }

    public class ChartData
    {
        public string[] Labels { get; set; } = Array.Empty<string>();
        public decimal[] Values { get; set; } = Array.Empty<decimal>();
    }
}
