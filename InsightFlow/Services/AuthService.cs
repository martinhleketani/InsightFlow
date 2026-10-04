using System.Net.Http.Json;
using InsightFlow.Models;
using Microsoft.Maui.Storage;

namespace InsightFlow.Services
{
    public class AuthService
    {
        private readonly ApiService _apiService;

        public AuthService(ApiService apiService)
        {
            _apiService = apiService;
        }


        // =========================================================
        // LOGIN
        // =========================================================

        public async Task<LoginResponse> LoginAsync(
            string companyEmail,
            string employeeId,
            string password)
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    CompanyEmail = companyEmail.Trim(),
                    EmployeeId = employeeId.Trim(),
                    Password = password
                };


                var response =
                    await _apiService.Client.PostAsJsonAsync(
                        "api/Auth/login",
                        loginRequest);


                // =================================================
                // LOGIN FAILED
                // =================================================

                if (!response.IsSuccessStatusCode)
                {
                    LoginResponse? errorResponse = null;

                    try
                    {
                        errorResponse =
                            await response.Content
                                .ReadFromJsonAsync<LoginResponse>();
                    }
                    catch
                    {
                        // Ignore JSON parsing failure.
                    }


                    return new LoginResponse
                    {
                        Success = false,

                        Message =
                            errorResponse?.Message
                            ?? "Login failed. Check your Company Email, Employee ID and Password."
                    };
                }


                // =================================================
                // READ LOGIN RESPONSE
                // =================================================

                var loginResponse =
                    await response.Content
                        .ReadFromJsonAsync<LoginResponse>();


                if (loginResponse == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message =
                            "The server returned an invalid response."
                    };
                }


                if (!loginResponse.Success)
                {
                    return new LoginResponse
                    {
                        Success = false,

                        Message =
                            string.IsNullOrWhiteSpace(
                                loginResponse.Message)
                                ? "Login failed."
                                : loginResponse.Message
                    };
                }


                if (string.IsNullOrWhiteSpace(
                        loginResponse.Token))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message =
                            "The server did not return an authentication token."
                    };
                }


                if (loginResponse.Employee == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message =
                            "The server did not return employee account information."
                    };
                }


                // =================================================
                // SAVE JWT
                // =================================================

                await SecureStorage.Default.SetAsync(
                    "auth_token",
                    loginResponse.Token);


                // =================================================
                // SAVE EMPLOYEE INFORMATION
                // =================================================

                LoginEmployee employee =
                    loginResponse.Employee;


                await SecureStorage.Default.SetAsync(
                    "employee_account_id",
                    employee.AccountId.ToString());


                await SecureStorage.Default.SetAsync(
                    "employee_id",
                    employee.EmployeeId ?? string.Empty);


                await SecureStorage.Default.SetAsync(
                    "employee_first_name",
                    employee.FirstName ?? string.Empty);


                await SecureStorage.Default.SetAsync(
                    "employee_last_name",
                    employee.LastName ?? string.Empty);


                await SecureStorage.Default.SetAsync(
                    "employee_email",
                    employee.CompanyEmail ?? string.Empty);


                await SecureStorage.Default.SetAsync(
                    "employee_role",
                    employee.Role ?? string.Empty);


                await SecureStorage.Default.SetAsync(
                    "employee_department",
                    employee.Department ?? string.Empty);


                await SecureStorage.Default.SetAsync(
                    "employee_department_id",
                    employee.DepartmentId.ToString());


                await SecureStorage.Default.SetAsync(
                    "employee_department_code",
                    employee.DepartmentCode ?? string.Empty);


                // =================================================
                // ADD JWT TO CURRENT API SERVICE
                // =================================================

                _apiService.SetAuthorizationToken(
                    loginResponse.Token);


                return loginResponse;
            }
            catch (HttpRequestException)
            {
                return new LoginResponse
                {
                    Success = false,

                    Message =
                        "Could not connect to the InsightFlow server. Make sure InsightFlow.API is running."
                };
            }
            catch (TaskCanceledException)
            {
                return new LoginResponse
                {
                    Success = false,

                    Message =
                        "The connection to the InsightFlow server timed out."
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,

                    Message =
                        $"An unexpected error occurred: {ex.Message}"
                };
            }
        }


        // =========================================================
        // RESTORE SESSION
        // =========================================================

        public async Task<bool> RestoreSessionAsync()
        {
            try
            {
                return await _apiService
                    .RestoreAuthorizationTokenAsync();
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // CHECK IF USER HAS SESSION
        // =========================================================

        public async Task<bool> HasSessionAsync()
        {
            try
            {
                string? token =
                    await SecureStorage.Default.GetAsync(
                        "auth_token");

                return !string.IsNullOrWhiteSpace(token);
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // GET CURRENT ROLE
        // =========================================================

        public async Task<string> GetCurrentRoleAsync()
        {
            try
            {
                return
                    await SecureStorage.Default.GetAsync(
                        "employee_role")
                    ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }


        // =========================================================
        // GET CURRENT EMPLOYEE ID
        // =========================================================

        public async Task<string> GetCurrentEmployeeIdAsync()
        {
            try
            {
                return
                    await SecureStorage.Default.GetAsync(
                        "employee_id")
                    ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }


        // =========================================================
        // GET CURRENT DEPARTMENT
        // =========================================================

        public async Task<string> GetCurrentDepartmentAsync()
        {
            try
            {
                return
                    await SecureStorage.Default.GetAsync(
                        "employee_department")
                    ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }


        // =========================================================
        // LOGOUT
        // =========================================================

        public void Logout()
        {
            _apiService.ClearSession();
        }
    }
}