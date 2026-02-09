// Shared types for Reports to avoid duplicate type definitions
namespace NUTRIBITE.Models.Reports
{
    public class SummaryCard
    {
        public string Title { get; set; } = "";
        public string Value { get; set; } = "";
        public string SubText { get; set; } = "";
        public string Icon { get; set; } = ""; // optional
    }

    // One shared TrendPoint used across reports (orders and revenue)
    public class TrendPoint
    {
        public string Label { get; set; } = "";
        public int Orders { get; set; }
        public decimal Revenue { get; set; }
    }

    public class AlertModel
    {
        public int? OrderId { get; set; }
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Time { get; set; }
        public string Severity { get; set; } = "medium";
    }
}