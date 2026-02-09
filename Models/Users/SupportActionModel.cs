using System;

namespace NUTRIBITE.Models.Users
{
    public class SupportActionModel
    {
        public int ActionId { get; set; }
        public int UserId { get; set; }
        public string Admin { get; set; } = "";         // admin username
        public DateTime Timestamp { get; set; }         // action time
        public string ActionType { get; set; } = "";    // e.g. "Note", "Unblock", "Warning"
        public string Note { get; set; } = "";          // free text
        public string Result { get; set; } = "";        // optional result/status
        public string Severity { get; set; } = "info";  // info / success / warning
    }
}
