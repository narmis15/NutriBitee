using System;

namespace NUTRIBITE.Models.Users
{
    // DTO for User Orders + Calorie view
    public class UserOrderCalorieModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public OrderItem[] Orders { get; set; } = Array.Empty<OrderItem>();
        public int TotalCalories { get; set; }
        public double DailyAverageCalories { get; set; }
    }

    public class OrderItem
    {
        public int OrderId { get; set; }
        public string MealName { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public int Calories { get; set; }
        public string Status { get; set; } = ""; // New/Accepted/Picked/Cancelled
    }
}
