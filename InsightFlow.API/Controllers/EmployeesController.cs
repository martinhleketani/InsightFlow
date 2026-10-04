using InsightFlow.API.Data;
using InsightFlow.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsightFlow.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator")]
    public class EmployeesController : ControllerBase
    {
        private readonly InsightFlowDbContext _context;
        private readonly PasswordHasher<Employee> _passwordHasher;

        public EmployeesController(InsightFlowDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Employee>();
        }

        // =====================================================
        // GET ALL EMPLOYEES
        // GET: api/Employees
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .Select(e => new
                {
                    accountId = e.AccountId,
                    employeeId = e.EmployeeId,
                    firstName = e.FirstName,
                    lastName = e.LastName,

                    fullName =
                        e.FirstName + " " + e.LastName,

                    companyEmail = e.CompanyEmail,

                    departmentId = e.DepartmentId,

                    department =
                        e.Department != null
                            ? e.Department.DepartmentName
                            : null,

                    departmentCode =
                        e.Department != null
                            ? e.Department.DepartmentCode
                            : null,

                    jobTitle = e.JobTitle,
                    role = e.Role,
                    isActive = e.IsActive,
                    dateCreated = e.DateCreated
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = employees.Count,
                employees
            });
        }

        // =====================================================
        // GET ONE EMPLOYEE
        // GET: api/Employees/1
        // =====================================================
        [HttpGet("{accountId:long}")]
        public async Task<IActionResult> GetEmployee(long accountId)
        {
            var employee = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .Where(e => e.AccountId == accountId)
                .Select(e => new
                {
                    accountId = e.AccountId,
                    employeeId = e.EmployeeId,
                    firstName = e.FirstName,
                    lastName = e.LastName,

                    fullName =
                        e.FirstName + " " + e.LastName,

                    companyEmail = e.CompanyEmail,

                    departmentId = e.DepartmentId,

                    department =
                        e.Department != null
                            ? e.Department.DepartmentName
                            : null,

                    departmentCode =
                        e.Department != null
                            ? e.Department.DepartmentCode
                            : null,

                    jobTitle = e.JobTitle,
                    role = e.Role,
                    isActive = e.IsActive,
                    dateCreated = e.DateCreated
                })
                .FirstOrDefaultAsync();

            if (employee == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Employee was not found."
                });
            }

            return Ok(new
            {
                success = true,
                employee
            });
        }

        // =====================================================
        // CREATE EMPLOYEE
        // POST: api/Employees
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> CreateEmployee(
            [FromBody] CreateEmployeeRequest request)
        {
            // -------------------------------------------------
            // Validate required fields
            // -------------------------------------------------
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.CompanyEmail) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.JobTitle))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "First name, last name, company email, " +
                        "password and job title are required."
                });
            }

            // -------------------------------------------------
            // Validate company email
            // -------------------------------------------------
            string email = request.CompanyEmail
                .Trim()
                .ToLowerInvariant();

            if (!email.EndsWith(
                    "@insightflow.co.za",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Employee email must use the " +
                        "@insightflow.co.za company domain."
                });
            }

            // -------------------------------------------------
            // Basic development password requirement
            // -------------------------------------------------
            if (request.Password.Length < 8)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Password must contain at least 8 characters."
                });
            }

            // -------------------------------------------------
            // Check duplicate email
            // -------------------------------------------------
            bool emailExists = await _context.Employees
                .AnyAsync(e => e.CompanyEmail.ToLower() == email);

            if (emailExists)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "An employee with this company email already exists."
                });
            }

            // -------------------------------------------------
            // Find department
            // -------------------------------------------------
            var department = await _context.Departments
                .FirstOrDefaultAsync(d =>
                    d.DepartmentId == request.DepartmentId &&
                    d.IsActive);

            if (department == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "The selected department does not exist " +
                        "or is inactive."
                });
            }

            // -------------------------------------------------
            // Validate role
            // -------------------------------------------------
            string role = request.Role?.Trim() ?? "Employee";

            string[] allowedRoles =
            {
                "Administrator",
                "Manager",
                "Employee"
            };

            var matchedRole = allowedRoles.FirstOrDefault(
                r => string.Equals(
                    r,
                    role,
                    StringComparison.OrdinalIgnoreCase));

            if (matchedRole == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Role must be Administrator, Manager or Employee."
                });
            }

            role = matchedRole;

            // -------------------------------------------------
            // Generate Employee ID
            //
            // Examples:
            // IF-SAL-0001
            // IF-FIN-0001
            // IF-IT-0001
            // -------------------------------------------------
            string employeeId =
                await GenerateEmployeeIdAsync(
                    department.DepartmentCode);

            // -------------------------------------------------
            // Create employee
            // -------------------------------------------------
            var employee = new Employee
            {
                EmployeeId = employeeId,

                FirstName =
                    request.FirstName.Trim(),

                LastName =
                    request.LastName.Trim(),

                CompanyEmail = email,

                DepartmentId =
                    department.DepartmentId,

                JobTitle =
                    request.JobTitle.Trim(),

                Role = role,

                IsActive = true,

                DateCreated = DateTime.UtcNow
            };

            // -------------------------------------------------
            // Hash password
            // -------------------------------------------------
            employee.PasswordHash =
                _passwordHasher.HashPassword(
                    employee,
                    request.Password);

            // -------------------------------------------------
            // Save employee
            // -------------------------------------------------
            _context.Employees.Add(employee);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEmployee),
                new
                {
                    accountId = employee.AccountId
                },
                new
                {
                    success = true,
                    message =
                        "Employee account created successfully.",

                    employee = new
                    {
                        accountId = employee.AccountId,
                        employeeId = employee.EmployeeId,
                        firstName = employee.FirstName,
                        lastName = employee.LastName,

                        fullName =
                            $"{employee.FirstName} {employee.LastName}",

                        companyEmail = employee.CompanyEmail,

                        departmentId =
                            employee.DepartmentId,

                        department =
                            department.DepartmentName,

                        departmentCode =
                            department.DepartmentCode,

                        jobTitle = employee.JobTitle,
                        role = employee.Role,
                        isActive = employee.IsActive,
                        dateCreated = employee.DateCreated
                    }
                });
        }

        // =====================================================
        // UPDATE EMPLOYEE
        // PUT: api/Employees/1
        // =====================================================
        [HttpPut("{accountId:long}")]
        public async Task<IActionResult> UpdateEmployee(
            long accountId,
            [FromBody] UpdateEmployeeRequest request)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e =>
                    e.AccountId == accountId);

            if (employee == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Employee was not found."
                });
            }

            // -------------------------------------------------
            // Validate fields
            // -------------------------------------------------
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.CompanyEmail) ||
                string.IsNullOrWhiteSpace(request.JobTitle))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "First name, last name, company email " +
                        "and job title are required."
                });
            }

            string email = request.CompanyEmail
                .Trim()
                .ToLowerInvariant();

            // -------------------------------------------------
            // Validate company domain
            // -------------------------------------------------
            if (!email.EndsWith(
                    "@insightflow.co.za",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Employee email must use the " +
                        "@insightflow.co.za company domain."
                });
            }

            // -------------------------------------------------
            // Check duplicate email
            // -------------------------------------------------
            bool emailExists = await _context.Employees
                .AnyAsync(e =>
                    e.AccountId != accountId &&
                    e.CompanyEmail.ToLower() == email);

            if (emailExists)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Another employee already uses this company email."
                });
            }

            // -------------------------------------------------
            // Validate department
            // -------------------------------------------------
            var department = await _context.Departments
                .FirstOrDefaultAsync(d =>
                    d.DepartmentId == request.DepartmentId &&
                    d.IsActive);

            if (department == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "The selected department does not exist " +
                        "or is inactive."
                });
            }

            // -------------------------------------------------
            // Validate role
            // -------------------------------------------------
            string role = request.Role?.Trim() ?? "Employee";

            string[] allowedRoles =
            {
                "Administrator",
                "Manager",
                "Employee"
            };

            var matchedRole = allowedRoles.FirstOrDefault(
                r => string.Equals(
                    r,
                    role,
                    StringComparison.OrdinalIgnoreCase));

            if (matchedRole == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Role must be Administrator, Manager or Employee."
                });
            }

            role = matchedRole;

            // -------------------------------------------------
            // If department changed, generate a new Employee ID
            // -------------------------------------------------
            if (employee.DepartmentId != department.DepartmentId)
            {
                employee.EmployeeId =
                    await GenerateEmployeeIdAsync(
                        department.DepartmentCode);
            }

            // -------------------------------------------------
            // Update employee
            // -------------------------------------------------
            employee.FirstName =
                request.FirstName.Trim();

            employee.LastName =
                request.LastName.Trim();

            employee.CompanyEmail = email;

            employee.DepartmentId =
                department.DepartmentId;

            employee.JobTitle =
                request.JobTitle.Trim();

            employee.Role = role;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message =
                    "Employee account updated successfully.",

                employee = new
                {
                    accountId = employee.AccountId,
                    employeeId = employee.EmployeeId,
                    firstName = employee.FirstName,
                    lastName = employee.LastName,

                    fullName =
                        $"{employee.FirstName} {employee.LastName}",

                    companyEmail = employee.CompanyEmail,

                    departmentId =
                        employee.DepartmentId,

                    department =
                        department.DepartmentName,

                    departmentCode =
                        department.DepartmentCode,

                    jobTitle = employee.JobTitle,
                    role = employee.Role,
                    isActive = employee.IsActive,
                    dateCreated = employee.DateCreated
                }
            });
        }

        // =====================================================
        // ENABLE / DISABLE EMPLOYEE
        // PATCH: api/Employees/1/status
        // =====================================================
        [HttpPatch("{accountId:long}/status")]
        public async Task<IActionResult> UpdateEmployeeStatus(
            long accountId,
            [FromBody] UpdateEmployeeStatusRequest request)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e =>
                    e.AccountId == accountId);

            if (employee == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Employee was not found."
                });
            }

            // Prevent administrator from disabling themselves
            var currentAccountId =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;

            if (long.TryParse(
                    currentAccountId,
                    out long loggedInAccountId) &&
                loggedInAccountId == employee.AccountId &&
                request.IsActive == false)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "You cannot disable your own account."
                });
            }

            employee.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,

                message = employee.IsActive
                    ? "Employee account enabled successfully."
                    : "Employee account disabled successfully.",

                employee = new
                {
                    accountId = employee.AccountId,
                    employeeId = employee.EmployeeId,
                    isActive = employee.IsActive
                }
            });
        }

        // =====================================================
        // RESET / CHANGE EMPLOYEE PASSWORD BY ADMIN
        // PATCH: api/Employees/1/password
        // =====================================================
        [HttpPatch("{accountId:long}/password")]
        public async Task<IActionResult> ChangeEmployeePassword(
            long accountId,
            [FromBody] AdminChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "New password is required."
                });
            }

            if (request.NewPassword.Length < 8)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Password must contain at least 8 characters."
                });
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e =>
                    e.AccountId == accountId);

            if (employee == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Employee was not found."
                });
            }

            employee.PasswordHash =
                _passwordHasher.HashPassword(
                    employee,
                    request.NewPassword);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message =
                    "Employee password changed successfully."
            });
        }

        // =====================================================
        // GENERATE EMPLOYEE ID
        // =====================================================
        private async Task<string> GenerateEmployeeIdAsync(
            string departmentCode)
        {
            string prefix =
                departmentCode.Trim().ToUpperInvariant();

            // Find all employee IDs belonging to this department.
            var existingIds = await _context.Employees
                .Where(e =>
                    e.EmployeeId.StartsWith(prefix + "-"))
                .Select(e => e.EmployeeId)
                .ToListAsync();

            int highestNumber = 0;

            foreach (string existingId in existingIds)
            {
                string numberPart =
                    existingId.Substring(
                        prefix.Length + 1);

                if (int.TryParse(
                        numberPart,
                        out int number) &&
                    number > highestNumber)
                {
                    highestNumber = number;
                }
            }

            int nextNumber = highestNumber + 1;

            string generatedId;

            do
            {
                generatedId =
                    $"{prefix}-{nextNumber:D4}";

                nextNumber++;
            }
            while (await _context.Employees
                .AnyAsync(e =>
                    e.EmployeeId == generatedId));

            return generatedId;
        }
    }

    // =========================================================
    // CREATE EMPLOYEE REQUEST
    // =========================================================
    public class CreateEmployeeRequest
    {
        public string FirstName { get; set; }
            = string.Empty;

        public string LastName { get; set; }
            = string.Empty;

        public string CompanyEmail { get; set; }
            = string.Empty;

        public string Password { get; set; }
            = string.Empty;

        public int DepartmentId { get; set; }

        public string JobTitle { get; set; }
            = string.Empty;

        public string Role { get; set; }
            = "Employee";
    }

    // =========================================================
    // UPDATE EMPLOYEE REQUEST
    // =========================================================
    public class UpdateEmployeeRequest
    {
        public string FirstName { get; set; }
            = string.Empty;

        public string LastName { get; set; }
            = string.Empty;

        public string CompanyEmail { get; set; }
            = string.Empty;

        public int DepartmentId { get; set; }

        public string JobTitle { get; set; }
            = string.Empty;

        public string Role { get; set; }
            = "Employee";
    }

    // =========================================================
    // UPDATE STATUS REQUEST
    // =========================================================
    public class UpdateEmployeeStatusRequest
    {
        public bool IsActive { get; set; }
    }

    // =========================================================
    // ADMIN PASSWORD CHANGE REQUEST
    // =========================================================
    public class AdminChangePasswordRequest
    {
        public string NewPassword { get; set; }
            = string.Empty;
    }
}