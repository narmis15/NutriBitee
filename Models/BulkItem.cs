using System;

namespace NUTRIBITE.Models
{
    public class BulkItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public decimal Price { get; set; }
        public bool IsVeg { get; set; }
        public string Category { get; set; } = ""; // Meals / Snacks / FoodBox
        public string? ImagePath { get; set; }      // Example: /images/bulk/...
        public int? MOQ { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
    }
}