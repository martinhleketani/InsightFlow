namespace InsightFlow.API.Models
{
    public class Employee
    {
        // Primary key
        public long AccountId { get; set; }

        // Unique company employee number
        public string EmployeeId { get; set; } = string.Empty;

        // Employee details
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        // Company email used for login
        public string CompanyEmail { get; set; } = string.Empty;

        // Secure hashed password
        public string PasswordHash { get; set; } = string.Empty;

        // Department foreign key
        public int DepartmentId { get; set; }

        // Employee's job title
        public string JobTitle { get; set; } = string.Empty;

        // Administrator, Manager or Employee
        public string Role { get; set; } = "Employee";

        // Determines whether the account can be used
        public bool IsActive { get; set; } = true;

        // Date account was created
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        // =====================================================
        // RELATIONSHIPS
        // =====================================================

        public Department? Department { get; set; }

        public ICollection<DailyRecord> DailyRecords { get; set; }
            = new List<DailyRecord>();

        public ICollection<PasswordResetRequest> PasswordResetRequests { get; set; }
            = new List<PasswordResetRequest>();

        public ICollection<AuditLog> AuditLogs { get; set; }
            = new List<AuditLog>();
    }
}