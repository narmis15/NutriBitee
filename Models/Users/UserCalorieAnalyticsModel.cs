using System;

namespace NUTRIBITE.Models.Users
{
    public class UserCalorieAnalyticsModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // Key metrics
        public double AverageDaily { get; set; }
        public int PeakDaily { get; set; }
        public int RecommendedDaily { get; set; }

        // Trend points for chart
        public CalorieTrendPoint[] Trend { get; set; } = Array.Empty<CalorieTrendPoint>();

        // Comparison: percentage of recommended (AverageDaily / RecommendedDaily * 100)
        public double ComparisonPercent { get; set; }

        // Informational alerts
        public AnalyticsAlert[] Alerts { get; set; } = Array.Empty<AnalyticsAlert>();
    }

    public class CalorieTrendPoint
    {
        public string Label { get; set; } = "";
        public int Calories { get; set; }
    }

    public class AnalyticsAlert
    {
        public string Level { get; set; } = "info"; // info / warning
        public string Message { get; set; } = "";
    }
}
