using InsightFlow.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InsightFlow.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            InsightFlowDbContext context,
            IConfiguration configuration)
        {
            // =====================================================
            // APPLY DATABASE MIGRATIONS
            // =====================================================

            await context.Database.MigrateAsync();


            // =====================================================
            // SEED DEPARTMENTS
            // =====================================================

            if (!await context.Departments.AnyAsync())
            {
                var departments =
                    new List<Department>
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
            // FIND MANAGEMENT DEPARTMENT
            // =====================================================

            Department? managementDepartment =
                await context.Departments
                    .FirstOrDefaultAsync(
                        department =>
                            department.DepartmentCode ==
                            "IF-MGT");


            if (managementDepartment == null)
            {
                throw new InvalidOperationException(
                    "Management department could not be found.");
            }


            // =====================================================
            // CHECK INITIAL ADMINISTRATOR
            // =====================================================

            bool adminExists =
                await context.Employees
                    .AnyAsync(
                        employee =>
                            employee.EmployeeId ==
                            "IF-MGT-0001"

                            ||

                            employee.CompanyEmail ==
                            "admin@insightflow.co.za");


            // =====================================================
            // CREATE INITIAL ADMINISTRATOR
            //
            // Only the first administrator is automatically created.
            //
            // Normal employees and managers must be created through
            // the Employee Management page by an Administrator.
            //
            // The initial administrator password is NOT stored in
            // source code. It is loaded from configuration.
            //
            // Development:
            // User Secrets -> SeedAdmin:Password
            //
            // Production:
            // Environment variable or another secure configuration
            // provider.
            // =====================================================

            if (!adminExists)
            {
                string? adminPassword =
                    configuration["SeedAdmin:Password"];


                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    throw new InvalidOperationException(
                        "The initial administrator password was not configured. " +
                        "Configure 'SeedAdmin:Password' using User Secrets " +
                        "or another secure configuration provider.");
                }


                var admin =
                    new Employee
                    {
                        EmployeeId =
                            "IF-MGT-0001",

                        FirstName =
                            "System",

                        LastName =
                            "Administrator",

                        CompanyEmail =
                            "admin@insightflow.co.za",

                        DepartmentId =
                            managementDepartment.DepartmentId,

                        JobTitle =
                            "System Administrator",

                        Role =
                            "Administrator",

                        IsActive =
                            true,

                        DateCreated =
                            DateTime.UtcNow
                    };


                // =================================================
                // HASH INITIAL ADMIN PASSWORD
                // =================================================

                var passwordHasher =
                    new PasswordHasher<Employee>();


                admin.PasswordHash =
                    passwordHasher.HashPassword(
                        admin,
                        adminPassword);


                await context.Employees.AddAsync(
                    admin);

                await context.SaveChangesAsync();
            }
        }
    }
}