using InsightFlow.Models;

namespace InsightFlow.Services
{
    public static class EmployeeAccountStore
    {
        // =========================================================
        // TEMPORARY DEVELOPMENT ACCOUNT STORAGE
        // =========================================================
        //
        // This is temporary in-memory storage.
        // Later accounts will be stored securely in SQL Server
        // and accessed through the ASP.NET Core API.
        //
        // =========================================================

        private static readonly List<EmployeeAccount> accounts = new()
        {
            // ADMIN DEVELOPMENT ACCOUNT
            new EmployeeAccount
            {
                AccountId = 1,
                EmployeeId = "IF-MGT-0001",
                FirstName = "System",
                LastName = "Administrator",
                CompanyEmail = "admin@insightflow.co.za",
                Password = "Admin123",
                Department = "Management",
                JobTitle = "System Administrator",
                Role = "Administrator",
                IsActive = true,
                DateCreated = DateTime.Now
            },

            // EMPLOYEE DEVELOPMENT ACCOUNT
            new EmployeeAccount
            {
                AccountId = 2,
                EmployeeId = "IF-SAL-0001",
                FirstName = "Development",
                LastName = "Employee",
                CompanyEmail = "employee@insightflow.co.za",
                Password = "Employee123",
                Department = "Sales",
                JobTitle = "Sales Representative",
                Role = "Employee",
                IsActive = true,
                DateCreated = DateTime.Now
            }
        };


        // =========================================================
        // GET ALL ACCOUNTS
        // =========================================================

        public static List<EmployeeAccount> GetAllAccounts()
        {
            return accounts
                .OrderBy(account => account.EmployeeId)
                .ToList();
        }


        // =========================================================
        // GET ACCOUNT BY EMPLOYEE ID
        // =========================================================

        public static EmployeeAccount? GetByEmployeeId(
            string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return null;
            }

            return accounts.FirstOrDefault(account =>
                account.EmployeeId.Equals(
                    employeeId,
                    StringComparison.OrdinalIgnoreCase));
        }


        // =========================================================
        // GET ACCOUNT BY EMAIL
        // =========================================================

        public static EmployeeAccount? GetByEmail(
            string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return accounts.FirstOrDefault(account =>
                account.CompanyEmail.Equals(
                    email,
                    StringComparison.OrdinalIgnoreCase));
        }


        // =========================================================
        // ADD EMPLOYEE ACCOUNT
        // =========================================================

        public static bool AddAccount(
            EmployeeAccount account)
        {
            if (account == null)
            {
                return false;
            }

            // Employee ID must be unique
            if (GetByEmployeeId(account.EmployeeId) != null)
            {
                return false;
            }

            // Company email must be unique
            if (GetByEmail(account.CompanyEmail) != null)
            {
                return false;
            }

            account.AccountId =
                GetNextAccountId();

            account.DateCreated =
                DateTime.Now;

            accounts.Add(account);

            return true;
        }


        // =========================================================
        // GENERATE NEXT ACCOUNT ID
        // =========================================================

        private static int GetNextAccountId()
        {
            if (accounts.Count == 0)
            {
                return 1;
            }

            return accounts.Max(account =>
                account.AccountId) + 1;
        }


        // =========================================================
        // UPDATE ACCOUNT
        // =========================================================

        public static bool UpdateAccount(
            EmployeeAccount updatedAccount)
        {
            EmployeeAccount? existing =
                accounts.FirstOrDefault(account =>
                    account.AccountId ==
                    updatedAccount.AccountId);

            if (existing == null)
            {
                return false;
            }

            existing.FirstName =
                updatedAccount.FirstName;

            existing.LastName =
                updatedAccount.LastName;

            existing.CompanyEmail =
                updatedAccount.CompanyEmail;

            existing.Department =
                updatedAccount.Department;

            existing.JobTitle =
                updatedAccount.JobTitle;

            existing.Role =
                updatedAccount.Role;

            existing.IsActive =
                updatedAccount.IsActive;

            return true;
        }


        // =========================================================
        // DISABLE ACCOUNT
        // =========================================================

        public static bool DisableAccount(
            string employeeId)
        {
            EmployeeAccount? account =
                GetByEmployeeId(employeeId);

            if (account == null)
            {
                return false;
            }

            account.IsActive = false;

            return true;
        }


        // =========================================================
        // ACTIVATE ACCOUNT
        // =========================================================

        public static bool ActivateAccount(
            string employeeId)
        {
            EmployeeAccount? account =
                GetByEmployeeId(employeeId);

            if (account == null)
            {
                return false;
            }

            account.IsActive = true;

            return true;
        }


        // =========================================================
        // SEARCH ACCOUNTS
        // =========================================================

        public static List<EmployeeAccount> SearchAccounts(
            string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return GetAllAccounts();
            }

            searchText =
                searchText.Trim();

            return accounts
                .Where(account =>
                    account.EmployeeId.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase)

                    ||

                    account.FirstName.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase)

                    ||

                    account.LastName.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase)

                    ||

                    account.CompanyEmail.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase)

                    ||

                    account.Department.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(account =>
                    account.EmployeeId)
                .ToList();
        }


        // =========================================================
        // ACTIVE ACCOUNT COUNT
        // =========================================================

        public static int GetActiveAccountCount()
        {
            return accounts.Count(account =>
                account.IsActive);
        }


        // =========================================================
        // DISABLED ACCOUNT COUNT
        // =========================================================

        public static int GetDisabledAccountCount()
        {
            return accounts.Count(account =>
                !account.IsActive);
        }


        // =========================================================
        // TOTAL ACCOUNT COUNT
        // =========================================================

        public static int GetTotalAccountCount()
        {
            return accounts.Count;
        }
    }
}