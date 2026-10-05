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

        private const string RememberEmployeeIdKey =
            "RememberEmployeeId";

        private const string RememberedEmployeeIdKey =
            "RememberedEmployeeId";

        private readonly ApiService _apiService;
        private readonly AuthService _authService;


        public MainPage()
        {
            InitializeComponent();

            _apiService = new ApiService();

            _authService =
                new AuthService(_apiService);

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

            // -----------------------------------------------------
            // READ AND CLEAN LOGIN VALUES
            // -----------------------------------------------------

            string email =
                CleanEmail(EmailEntry.Text);

            string employeeId =
                CleanEmployeeId(EmployeeIdEntry.Text);

            string password =
                CleanPassword(PasswordEntry.Text);


            // -----------------------------------------------------
            // REQUIRED FIELDS
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowLoginError(
                    "Enter your company email.");

                EmailEntry.Focus();

                return;
            }


            if (string.IsNullOrWhiteSpace(employeeId))
            {
                ShowLoginError(
                    "Enter your Employee ID.");

                EmployeeIdEntry.Focus();

                return;
            }


            if (string.IsNullOrWhiteSpace(password))
            {
                ShowLoginError(
                    "Enter your password.");

                PasswordEntry.Focus();

                return;
            }


            // -----------------------------------------------------
            // COMPANY EMAIL VALIDATION
            // -----------------------------------------------------

            if (!email.EndsWith(
                    CompanyDomain,
                    StringComparison.OrdinalIgnoreCase))
            {
                ShowLoginError(
                    "Access denied. Use your approved company email.");

                EmailEntry.Focus();

                return;
            }


            // -----------------------------------------------------
            // EMPLOYEE ID VALIDATION
            // -----------------------------------------------------

            if (!employeeId.StartsWith(
                    "IF-",
                    StringComparison.OrdinalIgnoreCase))
            {
                ShowLoginError(
                    "The Employee ID is not valid.");

                EmployeeIdEntry.Focus();

                return;
            }


            // -----------------------------------------------------
            // PUT CLEANED VALUES BACK INTO TEXTBOXES
            // -----------------------------------------------------

            EmailEntry.Text =
                email;

            EmployeeIdEntry.Text =
                employeeId;

            PasswordEntry.Text =
                password;


            // -----------------------------------------------------
            // DISABLE SIGN IN WHILE CONNECTING
            // -----------------------------------------------------

            SignInButton.IsEnabled =
                false;

            SignInButton.Text =
                "SIGNING IN...";


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
                        string.IsNullOrWhiteSpace(
                            result.Message)
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


                // =================================================
                // SUCCESSFUL LOGIN
                // =================================================

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

                            IsActive =
                                true
                        };


                    await Navigation.PushAsync(
                        new EmployeeDashboardPage(
                            employeeAccount));

                    return;
                }


                // =================================================
                // UNKNOWN ROLE
                // =================================================

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
                SignInButton.IsEnabled =
                    true;

                SignInButton.Text =
                    "SIGN IN";
            }
        }


        // =========================================================
        // CLEAN EMAIL
        // =========================================================

        private static string CleanEmail(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\t", string.Empty)
                .Trim()
                .ToLowerInvariant();
        }


        // =========================================================
        // CLEAN EMPLOYEE ID
        // =========================================================

        private static string CleanEmployeeId(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\t", string.Empty)
                .Trim()
                .ToUpperInvariant();
        }


        // =========================================================
        // CLEAN PASSWORD
        // =========================================================

        private static string CleanPassword(
            string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            // Remove accidental line breaks/tabs that can be copied
            // with credentials and remove surrounding whitespace.
            //
            // Internal spaces are NOT removed because they may be
            // part of a legitimate password.

            return value
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\t", string.Empty)
                .Trim();
        }


        // =========================================================
        // SUCCESSFUL LOGIN
        // =========================================================

        private void LoginSuccessful(
            string employeeId)
        {
            SaveEmployeeIdIfRequested(
                employeeId);

            // Password must never remain on the login page.
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
                    RememberEmployeeIdKey,
                    true);

                Preferences.Default.Set(
                    RememberedEmployeeIdKey,
                    employeeId);

                // Keep the remembered Employee ID visible.
                EmployeeIdEntry.Text =
                    employeeId;
            }
            else
            {
                Preferences.Default.Set(
                    RememberEmployeeIdKey,
                    false);

                Preferences.Default.Remove(
                    RememberedEmployeeIdKey);
            }
        }


        // =========================================================
        // LOAD REMEMBERED EMPLOYEE ID
        // =========================================================

        private void LoadRememberedEmployeeId()
        {
            bool rememberEmployee =
                Preferences.Default.Get(
                    RememberEmployeeIdKey,
                    false);


            if (!rememberEmployee)
            {
                RememberEmployeeCheckBox.IsChecked =
                    false;

                return;
            }


            string savedEmployeeId =
                Preferences.Default.Get(
                    RememberedEmployeeIdKey,
                    string.Empty);


            if (!string.IsNullOrWhiteSpace(
                    savedEmployeeId))
            {
                EmployeeIdEntry.Text =
                    CleanEmployeeId(
                        savedEmployeeId);
            }


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
                CleanEmail(
                    EmailEntry.Text);

            string employeeId =
                CleanEmployeeId(
                    EmployeeIdEntry.Text);


            // Put cleaned values back into the fields.
            EmailEntry.Text =
                email;

            EmployeeIdEntry.Text =
                employeeId;


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


            // Password Reset API will be connected later.
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