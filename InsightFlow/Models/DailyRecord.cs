namespace InsightFlow.Models
{
    public class DailyRecord
    {
        public int RecordId { get; set; }

        public DateTime Date { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public string EmployeeId { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string ActivityType { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal BusinessValue { get; set; }

        public int SecondaryMetric { get; set; }

        public string Status { get; set; } = string.Empty;

        public string BusinessImpact { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }
    }
}
