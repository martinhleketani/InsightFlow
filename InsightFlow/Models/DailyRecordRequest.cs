namespace InsightFlow.Models
{
    public class DailyRecordRequest
    {
        public DateTime RecordDate { get; set; }

        public string ActivityType { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal BusinessValue { get; set; }

        public int SecondaryMetric { get; set; }

        public string Status { get; set; } = string.Empty;

        public string BusinessImpact { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
    }
}