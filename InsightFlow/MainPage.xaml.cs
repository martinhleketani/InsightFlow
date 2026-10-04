using InsightFlow.Models;
using InsightFlow.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace InsightFlow
{
    public partial class MainPage : ContentPage
    {
        private const string CompanyDomain =
            "@insightflow.co.za";

        private readonly ApiService _apiService;
        private readonly AuthService _authService;

        public MainPage()
        {
            InitializeComponent();

            _apiService = new ApiService();
            _authService = new AuthService(_apiService);

            LoadRememberedEmployeeId();
        }

        // =========================================================
        // LOGIN
        // =========================================================

        private async void OnLoginClicked(
            object sender,
            EventArgs e)
        {
            HideLoginError();

            string email =
                EmailEntry.Text?.Trim()
                ?? string.Empty;

            string employeeId =
                EmployeeIdEntry.Text?.Trim().ToUpperInvariant()
                ?? string.Empty;

            string password =
                PasswordEntry.Text
                ?? string.Empty;

            // -----------------------------------------------------
            // REQUIRED FIELDS
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowLoginError(
                    "Enter your company email.");

                return;
            }

            if (string.IsNullOrWhiteSpace(employeeId))
            {
                ShowLoginError(
                    "Enter your Employee ID.");

                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowLoginError(
                    "Enter your password.");

                return;
            }

            // -----------------------------------------------------
            // COMPANY EMAIL
            // -----------------------------------------------------

            if (!email.EndsWith(
                    CompanyDomain,
                    StringComparison.OrdinalIgnoreCase))
            {
                ShowLoginError(
                    "Access denied. Use your approved company email.");

                return;
            }

            // -----------------------------------------------------
            // EMPLOYEE ID FORMAT
            // -----------------------------------------------------

            if (!employeeId.StartsWith(
                    "IF-",
                    StringComparison.OrdinalIgnoreCase))
            {
                ShowLoginError(
                    "The Employee ID is not valid.");

                return;
            }

            // -----------------------------------------------------
            // DISABLE BUTTON WHILE CONNECTING
            // -----------------------------------------------------

            SignInButton.IsEnabled = false;
            SignInButton.Text = "SIGNING IN...";

            try
            {
                // =================================================
                // REAL API LOGIN
                // =================================================

                LoginResponse result =
                    await _authService.LoginAsync(
                        email,
                        employeeId,
                        password);

                if (!result.Success)
                {
                    ShowLoginError(
                        string.IsNullOrWhiteSpace(result.Message)
                            ? "The credentials could not be verified."
                            : result.Message);

                    return;
                }

                if (result.Employee == null)
                {
                    ShowLoginError(
                        "Employee account information was not returned.");

                    return;
                }

                LoginSuccessful(
                    result.Employee.EmployeeId);

                // =================================================
                // ADMINISTRATOR
                // =================================================

                if (result.Employee.Role.Equals(
                        "Administrator",
                        StringComparison.OrdinalIgnoreCase))
                {
                    await Navigation.PushAsync(
                        new DashboardPage());

                    return;
                }

                // =================================================
                // MANAGER
                // =================================================

                if (result.Employee.Role.Equals(
                        "Manager",
                        StringComparison.OrdinalIgnoreCase))
                {
                    await Navigation.PushAsync(
                        new DashboardPage());

                    return;
                }

                // =================================================
                // EMPLOYEE
                // =================================================

                if (result.Employee.Role.Equals(
                        "Employee",
                        StringComparison.OrdinalIgnoreCase))
                {
                    var employeeAccount =
                        new EmployeeAccount
                        {
                            EmployeeId =
                                result.Employee.EmployeeId,

                            FirstName =
                                result.Employee.FirstName,

                            LastName =
                                result.Employee.LastName,

                            CompanyEmail =
                                result.Employee.CompanyEmail,

                            Department =
                                result.Employee.Department,

                            JobTitle =
                                result.Employee.JobTitle,

                            Role =
                                result.Employee.Role,

                            IsActive = true
                        };

                    await Navigation.PushAsync(
                        new EmployeeDashboardPage(
                            employeeAccount));

                    return;
                }

                ShowLoginError(
                    "Your account role is not supported.");
            }
            catch (Exception ex)
            {
                ShowLoginError(
                    $"Login failed: {ex.Message}");
            }
            finally
            {
                SignInButton.IsEnabled = true;
                SignInButton.Text = "SIGN IN";
            }
        }

        // =========================================================
        // SUCCESSFUL LOGIN
        // =========================================================

        private void LoginSuccessful(
            string employeeId)
        {
            SaveEmployeeIdIfRequested(
                employeeId);

            PasswordEntry.Text =
                string.Empty;

            PasswordEntry.IsPassword =
                true;

            ShowPasswordButton.Text =
                "Show";

            HideLoginError();
        }

        // =========================================================
        // SHOW / HIDE PASSWORD
        // =========================================================

        private void OnShowPasswordClicked(
            object sender,
            EventArgs e)
        {
            PasswordEntry.IsPassword =
                !PasswordEntry.IsPassword;

            ShowPasswordButton.Text =
                PasswordEntry.IsPassword
                    ? "Show"
                    : "Hide";
        }

        // =========================================================
        // REMEMBER EMPLOYEE ID
        // =========================================================

        private void SaveEmployeeIdIfRequested(
            string employeeId)
        {
            if (RememberEmployeeCheckBox.IsChecked)
            {
                Preferences.Default.Set(
                    "RememberEmployeeId",
                    true);

                Preferences.Default.Set(
                    "RememberedEmployeeId",
                    employeeId);
            }
            else
            {
                Preferences.Default.Set(
                    "RememberEmployeeId",
                    false);

                Preferences.Default.Remove(
                    "RememberedEmployeeId");
            }
        }

        private void LoadRememberedEmployeeId()
        {
            bool rememberEmployee =
                Preferences.Default.Get(
                    "RememberEmployeeId",
                    false);

            if (!rememberEmployee)
            {
                return;
            }

            string savedEmployeeId =
                Preferences.Default.Get(
                    "RememberedEmployeeId",
                    string.Empty);

            EmployeeIdEntry.Text =
                savedEmployeeId;

            RememberEmployeeCheckBox.IsChecked =
                true;
        }

        // =========================================================
        // FORGOT PASSWORD
        // =========================================================

        private async void OnForgotPasswordClicked(
            object sender,
            EventArgs e)
        {
            HideLoginError();

            string email =
                EmailEntry.Text?.Trim()
                ?? string.Empty;

            string employeeId =
                EmployeeIdEntry.Text?.Trim().ToUpperInvariant()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(employeeId))
            {
                await DisplayAlert(
                    "Password Reset",
                    "Enter your Company Email and Employee ID first.",
                    "OK");

                return;
            }

            if (!email.EndsWith(
                    CompanyDomain,
                    StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert(
                    "Password Reset",
                    "Password reset is available only for approved company accounts.",
                    "OK");

                return;
            }

            // We will connect this to the Password Reset API later.
            await DisplayAlert(
                "Password Reset",
                "Password reset will be connected to the InsightFlow API in the next backend stage.",
                "OK");
        }

        // =========================================================
        // ERROR DISPLAY
        // =========================================================

        private void ShowLoginError(
            string message)
        {
            LoginStatusLabel.Text =
                message;

            LoginErrorBorder.IsVisible =
                true;
        }

        private void HideLoginError()
        {
            LoginStatusLabel.Text =
                string.Empty;

            LoginErrorBorder.IsVisible =
                false;
        }
    }
}