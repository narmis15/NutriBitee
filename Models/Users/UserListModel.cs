using System;

namespace NUTRIBITE.Models.Users
{
    // Lightweight DTO used by UsersController -> Index view
    public class UserListModel
    {
        public int UserId { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public int OrdersCount { get; set; }
        public string Status { get; set; } = "Active"; // Active / Inactive / Blocked
        public DateTime RegisteredAt { get; set; } = DateTime.MinValue;
    }
}
