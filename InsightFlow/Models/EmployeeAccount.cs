namespace InsightFlow.Models
{
    public class EmployeeAccount
    {
        public int AccountId { get; set; }

        public string EmployeeId { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string CompanyEmail { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public string FullName
        {
            get
            {
                return $"{FirstName} {LastName}".Trim();
            }
        }

        public string AccountStatus
        {
            get
            {
                return IsActive ? "Active" : "Disabled";
            }
        }
    }
}