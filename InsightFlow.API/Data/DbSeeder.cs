using InsightFlow.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InsightFlow.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            InsightFlowDbContext context)
        {
            // =====================================================
            // APPLY PENDING DATABASE MIGRATIONS
            // =====================================================

            await context.Database.MigrateAsync();


            // =====================================================
            // 1. CREATE DEPARTMENTS IF THEY DO NOT EXIST
            // =====================================================

            if (!await context.Departments.AnyAsync())
            {
                var departments = new List<Department>
                {
                    new()
                    {
                        DepartmentName = "Sales",
                        DepartmentCode = "IF-SAL"
                    },

                    new()
                    {
                        DepartmentName = "Finance",
                        DepartmentCode = "IF-FIN"
                    },

                    new()
                    {
                        DepartmentName = "Human Resources",
                        DepartmentCode = "IF-HR"
                    },

                    new()
                    {
                        DepartmentName = "Information Technology",
                        DepartmentCode = "IF-IT"
                    },

                    new()
                    {
                        DepartmentName = "Marketing",
                        DepartmentCode = "IF-MKT"
                    },

                    new()
                    {
                        DepartmentName = "Operations",
                        DepartmentCode = "IF-OPS"
                    },

                    new()
                    {
                        DepartmentName = "Customer Service",
                        DepartmentCode = "IF-CS"
                    },

                    new()
                    {
                        DepartmentName = "Procurement",
                        DepartmentCode = "IF-PRC"
                    },

                    new()
                    {
                        DepartmentName = "Management",
                        DepartmentCode = "IF-MGT"
                    }
                };

                await context.Departments.AddRangeAsync(
                    departments);

                await context.SaveChangesAsync();
            }


            // =====================================================
            // 2. FIND REQUIRED DEPARTMENTS
            // =====================================================

            var managementDepartment =
                await context.Departments
                    .FirstOrDefaultAsync(d =>
                        d.DepartmentCode == "IF-MGT");

            if (managementDepartment == null)
            {
                throw new InvalidOperationException(
                    "Management department could not be found.");
            }


            var informationTechnologyDepartment =
                await context.Departments
                    .FirstOrDefaultAsync(d =>
                        d.DepartmentCode == "IF-IT");

            if (informationTechnologyDepartment == null)
            {
                throw new InvalidOperationException(
                    "Information Technology department could not be found.");
            }


            // =====================================================
            // PASSWORD HASHER
            // =====================================================

            var passwordHasher =
                new PasswordHasher<Employee>();


            // =====================================================
            // 3. CHECK AND CREATE ADMINISTRATOR
            // =====================================================

            var adminExists =
                await context.Employees.AnyAsync(e =>
                    e.EmployeeId == "IF-MGT-0001" ||
                    e.CompanyEmail ==
                        "admin@insightflow.co.za");


            if (!adminExists)
            {
                var admin = new Employee
                {
                    EmployeeId = "IF-MGT-0001",

                    FirstName = "System",

                    LastName = "Administrator",

                    CompanyEmail =
                        "admin@insightflow.co.za",

                    DepartmentId =
                        managementDepartment.DepartmentId,

                    JobTitle =
                        "System Administrator",

                    Role =
                        "Administrator",

                    IsActive = true,

                    DateCreated =
                        DateTime.UtcNow
                };


                // =================================================
                // HASH ADMIN PASSWORD
                // =================================================

                admin.PasswordHash =
                    passwordHasher.HashPassword(
                        admin,
                        "InsightAdmin123!"
                    );


                // =================================================
                // ADD ADMINISTRATOR
                // =================================================

                await context.Employees.AddAsync(admin);
            }


            // =====================================================
            // 4. CHECK AND CREATE DEMO EMPLOYEE
            // =====================================================

            var employeeExists =
                await context.Employees.AnyAsync(e =>
                    e.EmployeeId == "IF-IT-0001" ||
                    e.CompanyEmail ==
                        "employee@insightflow.co.za");


            if (!employeeExists)
            {
                var employee = new Employee
                {
                    EmployeeId = "IF-IT-0001",

                    FirstName = "Demo",

                    LastName = "Employee",

                    CompanyEmail =
                        "employee@insightflow.co.za",

                    DepartmentId =
                        informationTechnologyDepartment
                            .DepartmentId,

                    JobTitle =
                        "IT Employee",

                    Role =
                        "Employee",

                    IsActive = true,

                    DateCreated =
                        DateTime.UtcNow
                };


                // =================================================
                // HASH EMPLOYEE PASSWORD
                // =================================================

                employee.PasswordHash =
                    passwordHasher.HashPassword(
                        employee,
                        "InsightEmployee123!"
                    );


                // =================================================
                // ADD DEMO EMPLOYEE
                // =================================================

                await context.Employees.AddAsync(employee);
            }


            // =====================================================
            // 5. SAVE ALL CHANGES TO MYSQL
            // =====================================================

            await context.SaveChangesAsync();
        }
    }
}