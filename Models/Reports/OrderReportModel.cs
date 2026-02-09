namespace NUTRIBITE.Models.Reports
{
    public class OrderReportModel
    {
        public SummaryCard[] Summary { get; set; } = Array.Empty<SummaryCard>();
        public OrderRow[] Orders { get; set; } = Array.Empty<OrderRow>();
        public TrendPoint[] Trend { get; set; } = Array.Empty<TrendPoint>();
    }

    public class OrderRow
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = "";
        public int ItemsCount { get; set; }
        public string PickupSlot { get; set; } = "";
        public decimal Amount { get; set; }
        public int TotalCalories { get; set; }
        public string PaymentStatus { get; set; } = "";
        public string Status { get; set; } = ""; // New, Accepted, Picked, Cancelled, etc.
        public bool IsFlagged { get; set; }
    }
}
