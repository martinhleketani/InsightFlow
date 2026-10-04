using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InsightFlow.API.Data;
using InsightFlow.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InsightFlow.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly InsightFlowDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<Employee> _passwordHasher;

        public AuthController(
            InsightFlowDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<Employee>();
        }

        // =====================================================
        // LOGIN
        // POST: api/Auth/login
        // =====================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CompanyEmail) ||
                string.IsNullOrWhiteSpace(request.EmployeeId) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Company email, Employee ID and password are required."
                });
            }

            string email = request.CompanyEmail
                .Trim()
                .ToLowerInvariant();

            string employeeId = request.EmployeeId.Trim();

            // Find employee using BOTH email and employee ID
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e =>
                    e.CompanyEmail.ToLower() == email &&
                    e.EmployeeId == employeeId);

            if (employee == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid login details."
                });
            }

            // Disabled employees cannot log in
            if (!employee.IsActive)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "This employee account is disabled."
                });
            }

            // Verify hashed password
            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    employee,
                    employee.PasswordHash,
                    request.Password);

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid login details."
                });
            }

            // Generate JWT
            string token = GenerateJwtToken(employee);

            int expiryMinutes =
                _configuration.GetValue<int?>(
                    "Jwt:ExpiryMinutes") ?? 60;

            DateTime expiresAt =
                DateTime.UtcNow.AddMinutes(expiryMinutes);

            return Ok(new
            {
                success = true,
                message = "Login successful.",

                token,
                expiresAt,

                employee = new
                {
                    accountId = employee.AccountId,
                    employeeId = employee.EmployeeId,
                    firstName = employee.FirstName,
                    lastName = employee.LastName,

                    fullName =
                        $"{employee.FirstName} {employee.LastName}",

                    companyEmail = employee.CompanyEmail,
                    jobTitle = employee.JobTitle,
                    role = employee.Role,

                    departmentId = employee.DepartmentId,

                    department =
                        employee.Department?.DepartmentName,

                    departmentCode =
                        employee.Department?.DepartmentCode
                }
            });
        }

        // =====================================================
        // GENERATE JWT TOKEN
        // =====================================================
        private string GenerateJwtToken(Employee employee)
        {
            string jwtKey =
                _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT Key is missing.");

            string jwtIssuer =
                _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT Issuer is missing.");

            string jwtAudience =
                _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT Audience is missing.");

            int expiryMinutes =
                _configuration.GetValue<int?>(
                    "Jwt:ExpiryMinutes") ?? 60;

            var claims = new List<Claim>
            {
                new(
                    JwtRegisteredClaimNames.Sub,
                    employee.AccountId.ToString()),

                new(
                    ClaimTypes.NameIdentifier,
                    employee.AccountId.ToString()),

                new(
                    ClaimTypes.Name,
                    $"{employee.FirstName} {employee.LastName}"),

                new(
                    ClaimTypes.Email,
                    employee.CompanyEmail),

                new(
                    ClaimTypes.Role,
                    employee.Role),

                new(
                    "employeeId",
                    employee.EmployeeId),

                new(
                    "departmentId",
                    employee.DepartmentId.ToString()),

                new(
                    "department",
                    employee.Department?.DepartmentName
                    ?? string.Empty),

                new(
                    "departmentCode",
                    employee.Department?.DepartmentCode
                    ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow
                    .AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }

    // =========================================================
    // LOGIN REQUEST
    // =========================================================
    public class LoginRequest
    {
        public string CompanyEmail { get; set; }
            = string.Empty;

        public string EmployeeId { get; set; }
            = string.Empty;

        public string Password { get; set; }
            = string.Empty;
    }
}