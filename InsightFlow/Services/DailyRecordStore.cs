using InsightFlow.Models;

namespace InsightFlow.Services
{
    public static class DailyRecordStore
    {
        private static readonly List<DailyRecord> records = new();

        public static IReadOnlyList<DailyRecord> Records =>
            records.AsReadOnly();

        public static void AddRecord(DailyRecord record)
        {
            record.RecordId = records.Count + 1;
            record.SubmittedAt = DateTime.Now;

            records.Add(record);
        }

        public static List<DailyRecord> GetEmployeeRecords(
            string employeeId)
        {
            return records
                .Where(record =>
                    record.EmployeeId.Equals(
                        employeeId,
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(record => record.SubmittedAt)
                .ToList();
        }

        public static List<DailyRecord> GetAllRecords()
        {
            return records
                .OrderByDescending(record => record.SubmittedAt)
                .ToList();
        }

        public static int GetEmployeeRecordCount(
            string employeeId)
        {
            return records.Count(record =>
                record.EmployeeId.Equals(
                    employeeId,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}