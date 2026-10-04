namespace InsightFlow.API.Models
{
    public class PasswordResetRequest
    {
        // MySQL BIGINT AUTO_INCREMENT
        public long ResetRequestId { get; set; }

        // Employee requesting the password reset
        public long EmployeeAccountId { get; set; }

        // Store a hash of the reset token, never the actual token
        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relationship to Employee
        public Employee? Employee { get; set; }
    }
}