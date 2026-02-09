namespace NUTRIBITE.Models.Reports
{
    public class PaymentReportModel
    {
        public PaymentRow[] Payments { get; set; } = Array.Empty<PaymentRow>();
        public int TotalSuccess { get; set; }
        public int TotalFailed { get; set; }
        public int TotalRefunded { get; set; }
        public int TotalCancelled { get; set; }
    }

    public class PaymentRow
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string CustomerName { get; set; } = "";
        public string Method { get; set; } = ""; // e.g. UPI, Card, COD
        public decimal Amount { get; set; }
        public string Status { get; set; } = ""; // Success, Failed, Refunded, Cancelled
        public string GatewayRef { get; set; } = "";
        public string Notes { get; set; } = "";
    }
}
