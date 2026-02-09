namespace NUTRIBITE.Models.Reports
{
    public class DashboardModel
    {
        public SummaryCard[] Summary { get; set; } = Array.Empty<SummaryCard>();
        public TrendPoint[] Trend { get; set; } = Array.Empty<TrendPoint>();
        public AlertModel[] Alerts { get; set; } = Array.Empty<AlertModel>();
    }
}