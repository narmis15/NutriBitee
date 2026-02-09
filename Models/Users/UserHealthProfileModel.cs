using System;

namespace NUTRIBITE.Models.Users
{
    public class UserHealthProfileModel
    {
        // Basic identity
        public int UserId { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";

        // Account info
        public string Status { get; set; } = "Active"; // Active / At Risk / Inactive / Blocked
        public DateTime RegisteredAt { get; set; }

        // Dietary
        public string DietType { get; set; } = "Veg"; // Veg/Vegan/Non-veg/Jain
        public int CalorieGoal { get; set; } = 2000;
        public int TodayCalories { get; set; } = 0;
        public int WeeklyCaloriesAverage { get; set; } = 0;

        // Orders / activity
        public int OrdersCount { get; set; }

        // Trend: last 7 days (label + calories)
        public TrendPoint[] WeeklyTrend { get; set; } = Array.Empty<TrendPoint>();

        // free-form admin note (optional)
        public string AdminNote { get; set; } = "";
    }

    public class TrendPoint
    {
        public string Label { get; set; } = "";
        public int Calories { get; set; }
    }
}
