using System;

namespace NUTRIBITE.Models.Users
{
    // Response DTO for Coupon Misuse page
    public class CouponMisuseModel
    {
        public MisuseSummary Summary { get; set; } = new MisuseSummary();
        public CouponRow[] Rows { get; set; } = Array.Empty<CouponRow>();
    }

    public class MisuseSummary
    {
        public int TotalUsersObserved { get; set; }
        public int SuspiciousCount { get; set; }
        public double AvgMisuseScore { get; set; } // 0-100
    }

    public class CouponRow
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string CouponCode { get; set; } = "";
        public int Uses { get; set; }
        public int Cancels { get; set; }
        public DateTime LastUsed { get; set; }
        public double MisuseScore { get; set; } // 0-100
        public string Status { get; set; } = ""; // e.g. "Normal","Suspicious","Flagged"
    }
}
