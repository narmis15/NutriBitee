using System;

namespace NUTRIBITE.Models.Users
{
    // DTO describing a blocked/restricted account
    public class BlockedUserModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime BlockedAt { get; set; }
        public DateTime? ExpiresAt { get; set; } // null = indefinite
        public string BlockedBy { get; set; } = "";
        public string AdminNote { get; set; } = "";
        public bool IsActiveBlock => ExpiresAt == null || ExpiresAt > DateTime.UtcNow;
    }
}
