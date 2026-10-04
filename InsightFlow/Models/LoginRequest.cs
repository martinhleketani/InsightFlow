namespace InsightFlow.Models
{
    public class LoginRequest
    {
        public string CompanyEmail { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}