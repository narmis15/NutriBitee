namespace NUTRIBITE.Models
{
    public class FoodCategory
    {
        public int cid { get; set; }                 // PK
        public string ProductCategory { get; set; } = "";
        public string ProductPic { get; set; } = "";
        public string MealCategory { get; set; } = "";
        public string MealPic { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}


