namespace InsightFlow.API.Models
{
    public class AuditLog
    {
        // MySQL BIGINT AUTO_INCREMENT
        public long AuditLogId { get; set; }

        // Employee who performed the action
        public long? EmployeeAccountId { get; set; }

        // Example: Login, CreateRecord, UpdateEmployee, DisableAccount
        public string Action { get; set; } = string.Empty;

        // Example: Employee, DailyRecord
        public string EntityType { get; set; } = string.Empty;

        // ID of the affected database record
        public long? EntityId { get; set; }

        public string Details { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relationship to Employee
        public Employee? Employee { get; set; }
    }
}