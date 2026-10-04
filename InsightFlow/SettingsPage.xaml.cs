using System.Net;
using System.Net.Http.Headers;

namespace InsightFlow
{
    public partial class SettingsPage : ContentPage
    {
        private const string ApiBaseUrl =
            "https://localhost:7212/";

        private readonly HttpClient _httpClient;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public SettingsPage()
        {
            InitializeComponent();

            _httpClient =
                new HttpClient
                {
                    BaseAddress =
                        new Uri(ApiBaseUrl),

                    Timeout =
                        TimeSpan.FromSeconds(10)
                };
        }


        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadSettingsAsync();
        }


        // =========================================================
        // LOAD SETTINGS
        // =========================================================

        private async Task LoadSettingsAsync()
        {
            try
            {
                SetLoading(true);

                HideMessage();

                await LoadCurrentAccountAsync();

                LastUpdatedLabel.Text =
                    $"Settings refreshed: {DateTime.Now:dd MMM yyyy, HH:mm}";
            }
            catch (Exception ex)
            {
                ShowMessage(
                    $"Unable to load settings: {ex.Message}",
                    false);
            }
            finally
            {
                SetLoading(false);
            }
        }


        // =========================================================
        // CURRENT ACCOUNT
        // =========================================================

        private async Task LoadCurrentAccountAsync()
        {
            string? firstName =
                await SecureStorage.Default.GetAsync(
                    "employee_first_name");


            string? lastName =
                await SecureStorage.Default.GetAsync(
                    "employee_last_name");


            string? employeeId =
                await SecureStorage.Default.GetAsync(
                    "employee_id");


            string? email =
                await SecureStorage.Default.GetAsync(
                    "employee_email");


            string? department =
                await SecureStorage.Default.GetAsync(
                    "employee_department");


            string? role =
                await SecureStorage.Default.GetAsync(
                    "employee_role");


            string? token =
                await SecureStorage.Default.GetAsync(
                    "auth_token");


            string fullName =
                $"{firstName} {lastName}".Trim();


            CurrentEmployeeNameLabel.Text =
                string.IsNullOrWhiteSpace(fullName)
                    ? "Not available"
                    : fullName;


            CurrentEmployeeIdLabel.Text =
                string.IsNullOrWhiteSpace(employeeId)
                    ? "Not available"
                    : employeeId;


            CurrentEmployeeEmailLabel.Text =
                string.IsNullOrWhiteSpace(email)
                    ? "Not available"
                    : email;


            CurrentDepartmentLabel.Text =
                string.IsNullOrWhiteSpace(department)
                    ? "Not available"
                    : department;


            CurrentRoleLabel.Text =
                string.IsNullOrWhiteSpace(role)
                    ? "Not available"
                    : role;


            if (string.IsNullOrWhiteSpace(token))
            {
                SessionStatusLabel.Text =
                    "No active session";

                SessionStatusLabel.TextColor =
                    Color.FromArgb("#DC2626");
            }
            else
            {
                SessionStatusLabel.Text =
                    "Authenticated";

                SessionStatusLabel.TextColor =
                    Color.FromArgb("#16A34A");
            }
        }


        // =========================================================
        // TEST API CONNECTION
        // =========================================================

        private async void OnTestApiClicked(
            object sender,
            EventArgs e)
        {
            await TestApiConnectionAsync();
        }


        private async Task TestApiConnectionAsync()
        {
            try
            {
                SetLoading(true);

                HideMessage();


                string? token =
                    await SecureStorage.Default.GetAsync(
                        "auth_token");


                if (string.IsNullOrWhiteSpace(token))
                {
                    ApiStatusLabel.Text =
                        "Not authenticated";

                    ApiStatusLabel.TextColor =
                        Color.FromArgb("#DC2626");


                    ApiLastCheckLabel.Text =
                        DateTime.Now.ToString(
                            "dd MMM yyyy HH:mm");


                    ShowMessage(
                        "No authentication token was found. Please sign in again.",
                        false);

                    return;
                }


                _httpClient
                    .DefaultRequestHeaders
                    .Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        token);


                /*
                 * DailyRecords/my is used because it already exists
                 * and is available to an authenticated employee.
                 *
                 * This means the API test works for Administrator,
                 * Manager and Employee accounts.
                 */

                using HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        "api/DailyRecords/my");


                ApiLastCheckLabel.Text =
                    DateTime.Now.ToString(
                        "dd MMM yyyy HH:mm");


                if (response.IsSuccessStatusCode)
                {
                    ApiStatusLabel.Text =
                        "Connected";

                    ApiStatusLabel.TextColor =
                        Color.FromArgb("#16A34A");


                    ShowMessage(
                        "InsightFlow successfully connected to the API.",
                        true);

                    return;
                }


                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    ApiStatusLabel.Text =
                        "Authentication failed";

                    ApiStatusLabel.TextColor =
                        Color.FromArgb("#DC2626");


                    ShowMessage(
                        "The API is reachable, but the current session is not authorised. Sign in again.",
                        false);

                    return;
                }


                if (response.StatusCode ==
                    HttpStatusCode.Forbidden)
                {
                    ApiStatusLabel.Text =
                        "Access denied";

                    ApiStatusLabel.TextColor =
                        Color.FromArgb("#DC2626");


                    ShowMessage(
                        "The API is reachable, but access to the test endpoint was denied.",
                        false);

                    return;
                }


                ApiStatusLabel.Text =
                    $"API error {(int)response.StatusCode}";

                ApiStatusLabel.TextColor =
                    Color.FromArgb("#DC2626");


                ShowMessage(
                    $"The API responded with HTTP {(int)response.StatusCode}.",
                    false);
            }
            catch (TaskCanceledException)
            {
                ApiStatusLabel.Text =
                    "Timed out";

                ApiStatusLabel.TextColor =
                    Color.FromArgb("#DC2626");


                ApiLastCheckLabel.Text =
                    DateTime.Now.ToString(
                        "dd MMM yyyy HH:mm");


                ShowMessage(
                    "The API connection timed out. Make sure InsightFlow.API is running.",
                    false);
            }
            catch (HttpRequestException)
            {
                ApiStatusLabel.Text =
                    "Disconnected";

                ApiStatusLabel.TextColor =
                    Color.FromArgb("#DC2626");


                ApiLastCheckLabel.Text =
                    DateTime.Now.ToString(
                        "dd MMM yyyy HH:mm");


                ShowMessage(
                    "Could not connect to https://localhost:7212/. Make sure InsightFlow.API is running.",
                    false);
            }
            catch (Exception ex)
            {
                ApiStatusLabel.Text =
                    "Connection error";

                ApiStatusLabel.TextColor =
                    Color.FromArgb("#DC2626");


                ApiLastCheckLabel.Text =
                    DateTime.Now.ToString(
                        "dd MMM yyyy HH:mm");


                ShowMessage(
                    $"API connection test failed: {ex.Message}",
                    false);
            }
            finally
            {
                SetLoading(false);
            }
        }


        // =========================================================
        // EMPLOYEE MANAGEMENT
        // =========================================================

        private async void OnEmployeeManagementClicked(
            object sender,
            EventArgs e)
        {
            string? role =
                await SecureStorage.Default.GetAsync(
                    "employee_role");


            if (!string.Equals(
                    role,
                    "Administrator",
                    StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert(
                    "Administrator Required",
                    "Only an Administrator can access Employee Management.",
                    "OK");

                return;
            }


            await Navigation.PushAsync(
                new EmployeeManagementPage());
        }


        // =========================================================
        // REFRESH
        // =========================================================

        private async void OnRefreshClicked(
            object sender,
            EventArgs e)
        {
            await LoadSettingsAsync();
        }


        // =========================================================
        // LOGOUT
        // =========================================================

        private async void OnLogoutClicked(
            object sender,
            EventArgs e)
        {
            bool logout =
                await DisplayAlert(
                    "Log Out",
                    "Are you sure you want to log out of InsightFlow?",
                    "Log Out",
                    "Cancel");


            if (!logout)
            {
                return;
            }


            /*
             * Remove the session information saved by InsightFlow.
             */

            SecureStorage.Default.Remove(
                "auth_token");

            SecureStorage.Default.Remove(
                "employee_id");

            SecureStorage.Default.Remove(
                "employee_first_name");

            SecureStorage.Default.Remove(
                "employee_last_name");

            SecureStorage.Default.Remove(
                "employee_email");

            SecureStorage.Default.Remove(
                "employee_department");

            SecureStorage.Default.Remove(
                "employee_role");

            SecureStorage.Default.Remove(
                "employee_account_id");

            SecureStorage.Default.Remove(
                "employee_department_id");

            SecureStorage.Default.Remove(
                "employee_department_code");


            _httpClient
                .DefaultRequestHeaders
                .Authorization = null;


            if (Application.Current != null)
            {
                Application.Current.MainPage =
                    new NavigationPage(
                        new MainPage());
            }
        }


        // =========================================================
        // LOADING
        // =========================================================

        private void SetLoading(
            bool loading)
        {
            LoadingIndicator.IsVisible =
                loading;

            LoadingIndicator.IsRunning =
                loading;
        }


        // =========================================================
        // MESSAGE
        // =========================================================

        private void ShowMessage(
            string message,
            bool success)
        {
            MessageLabel.Text =
                message;


            if (success)
            {
                MessageBorder.BackgroundColor =
                    Color.FromArgb("#F0FDF4");

                MessageBorder.Stroke =
                    Color.FromArgb("#BBF7D0");

                MessageLabel.TextColor =
                    Color.FromArgb("#166534");
            }
            else
            {
                MessageBorder.BackgroundColor =
                    Color.FromArgb("#FEF2F2");

                MessageBorder.Stroke =
                    Color.FromArgb("#FECACA");

                MessageLabel.TextColor =
                    Color.FromArgb("#DC2626");
            }


            MessageBorder.IsVisible =
                true;
        }


        private void HideMessage()
        {
            MessageLabel.Text =
                string.Empty;

            MessageBorder.IsVisible =
                false;
        }


        // =========================================================
        // BACK
        // =========================================================

        private async void OnBackClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}