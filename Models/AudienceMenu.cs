using System.ComponentModel.DataAnnotations;

namespace NUTRIBITE.Models
{
    public class AudienceMenu
    {
        [Key]   // ⭐ THIS FIXES YOUR ERROR
        public int MenuId { get; set; }

        public string MenuName { get; set; }

        public string AudienceType { get; set; }

        public int Protein { get; set; }

        public int Calories { get; set; }

        public int Price { get; set; }

        public string ImagePath { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}