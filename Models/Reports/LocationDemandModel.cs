namespace NUTRIBITE.Models.Reports
{
    public class LocationDemandModel
    {
        public string City { get; set; } = "";
        public string Region { get; set; } = "";
        public int OrdersCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class LocationAnalyticsModel
    {
        public string SelectedCity { get; set; } = "All";
        public string[] Cities { get; set; } = Array.Empty<string>();
        public LocationDemandModel[] Locations { get; set; } = Array.Empty<LocationDemandModel>();
        public ChartPoint[] Chart { get; set; } = Array.Empty<ChartPoint>();
    }

    public class ChartPoint
    {
        public string Label { get; set; } = "";
        public int Value { get; set; }
    }
}