namespace InsightFlow.Models
{
    public class LoginResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public LoginEmployee? Employee { get; set; }
    }

    public class LoginEmployee
    {
        public long AccountId { get; set; }

        public string EmployeeId { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string CompanyEmail { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public int DepartmentId { get; set; }

        public string Department { get; set; } = string.Empty;

        public string DepartmentCode { get; set; } = string.Empty;
    }
}