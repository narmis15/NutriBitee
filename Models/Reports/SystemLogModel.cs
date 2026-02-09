namespace NUTRIBITE.Models.Reports
{
    public class SystemLogModel
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = "";         // e.g. Info, Warning, Error
        public string Source { get; set; } = "";        // component name
        public string Message { get; set; } = "";
        public string Details { get; set; } = "";       // stack trace or JSON payload
        public string User { get; set; } = "";          // optional username or system
    }
}
