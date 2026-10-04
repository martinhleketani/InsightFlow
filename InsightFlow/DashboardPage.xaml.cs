using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InsightFlow
{
    public partial class DashboardPage : ContentPage
    {
        private const string ApiBaseUrl =
            "https://localhost:7212/";

        private readonly HttpClient _httpClient;

        private readonly List<DashboardEmployeeDto>
            _employees = new();

        private readonly List<DashboardDailyRecordDto>
            _records = new();

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public DashboardPage()
        {
            InitializeComponent();

            _httpClient =
                new HttpClient
                {
                    BaseAddress =
                        new Uri(ApiBaseUrl),

                    Timeout =
                        TimeSpan.FromSeconds(30)
                };

            CurrentDateLabel.Text =
                DateTime.Now.ToString(
                    "dddd, dd MMMM yyyy");
        }


        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadDashboardAsync();
        }


        // =========================================================
        // AUTHENTICATION
        // =========================================================

        private async Task<bool>
            ConfigureAuthenticationAsync()
        {
            try
            {
                string? token =
                    await SecureStorage.Default.GetAsync(
                        "auth_token");

                if (string.IsNullOrWhiteSpace(token))
                {
                    _httpClient
                        .DefaultRequestHeaders
                        .Authorization = null;

                    ShowError(
                        "You are not signed in. Please log in again.");

                    return false;
                }

                _httpClient
                    .DefaultRequestHeaders
                    .Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        token);

                return true;
            }
            catch
            {
                _httpClient
                    .DefaultRequestHeaders
                    .Authorization = null;

                ShowError(
                    "Unable to restore your login session.");

                return false;
            }
        }


        // =========================================================
        // CURRENT ROLE
        // =========================================================

        private static async Task<string>
            GetCurrentRoleAsync()
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
        // LOAD DASHBOARD
        // =========================================================

        private async Task LoadDashboardAsync()
        {
            try
            {
                SetLoading(true);

                HideError();

                _employees.Clear();
                _records.Clear();

                ResetDashboard();


                bool authenticated =
                    await ConfigureAuthenticationAsync();

                if (!authenticated)
                {
                    return;
                }


                string role =
                    await GetCurrentRoleAsync();


                // =================================================
                // DAILY RECORDS
                //
                // Both Administrator and Manager are allowed to
                // access company DailyRecords.
                // =================================================

                bool recordsLoaded =
                    await LoadDailyRecordsAsync();

                if (!recordsLoaded)
                {
                    return;
                }


                // =================================================
                // EMPLOYEE ACCOUNTS
                //
                // Employee account management is Administrator-only.
                //
                // Administrators can load the complete employee list.
                //
                // Managers must NOT call the Administrator-only
                // Employees endpoint.
                // =================================================

                if (string.Equals(
                        role,
                        "Administrator",
                        StringComparison.OrdinalIgnoreCase))
                {
                    await LoadEmployeesAsync();
                }


                CalculateDashboard(
                    role);

                LoadRecentRecords();
            }
            catch (HttpRequestException)
            {
                ShowError(
                    "Unable to connect to the InsightFlow API. " +
                    "Make sure InsightFlow.API is running.");
            }
            catch (TaskCanceledException)
            {
                ShowError(
                    "The InsightFlow API took too long to respond.");
            }
            catch (JsonException ex)
            {
                ShowError(
                    $"The API returned data in an unexpected format: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError(
                    $"Unable to load dashboard: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }


        // =========================================================
        // LOAD EMPLOYEES
        // ADMINISTRATOR ONLY
        // =========================================================

        private async Task<bool> LoadEmployeesAsync()
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(
                    "api/Employees");


            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                ShowError(
                    "Your session has expired. Please sign in again.");

                return false;
            }


            if (response.StatusCode ==
                HttpStatusCode.Forbidden)
            {
                // Do not expose Administrator-only employee data
                // to Manager accounts.
                return false;
            }


            if (!response.IsSuccessStatusCode)
            {
                string error =
                    await ReadErrorMessageAsync(
                        response);

                ShowError(
                    string.IsNullOrWhiteSpace(error)
                        ? "Unable to load employees."
                        : error);

                return false;
            }


            string json =
                await response.Content
                    .ReadAsStringAsync();


            List<DashboardEmployeeDto> employees =
                DeserializeList<DashboardEmployeeDto>(
                    json,
                    "employees",
                    "data",
                    "items",
                    "results");


            _employees.Clear();

            _employees.AddRange(
                employees);


            return true;
        }


        // =========================================================
        // LOAD DAILY RECORDS
        // ADMINISTRATOR + MANAGER
        // =========================================================

        private async Task<bool> LoadDailyRecordsAsync()
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(
                    "api/DailyRecords");


            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                ShowError(
                    "Your session has expired. Please sign in again.");

                return false;
            }


            if (response.StatusCode ==
                HttpStatusCode.Forbidden)
            {
                ShowError(
                    "Administrator or Manager access is required " +
                    "to view company records.");

                return false;
            }


            if (!response.IsSuccessStatusCode)
            {
                string error =
                    await ReadErrorMessageAsync(
                        response);

                ShowError(
                    string.IsNullOrWhiteSpace(error)
                        ? "Unable to load company records."
                        : error);

                return false;
            }


            string json =
                await response.Content
                    .ReadAsStringAsync();


            List<DashboardDailyRecordDto> records =
                DeserializeList<DashboardDailyRecordDto>(
                    json,
                    "records",
                    "dailyRecords",
                    "data",
                    "items",
                    "results");


            _records.Clear();

            _records.AddRange(
                records);


            return true;
        }


        // =========================================================
        // FLEXIBLE JSON LIST DESERIALIZATION
        // =========================================================

        private static List<T> DeserializeList<T>(
            string json,
            params string[] possiblePropertyNames)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<T>();
            }


            using JsonDocument document =
                JsonDocument.Parse(json);

            JsonElement root =
                document.RootElement;


            // =====================================================
            // DIRECT ARRAY
            // =====================================================

            if (root.ValueKind ==
                JsonValueKind.Array)
            {
                return
                    JsonSerializer.Deserialize<List<T>>(
                        root.GetRawText(),
                        JsonOptions)

                    ?? new List<T>();
            }


            // =====================================================
            // WRAPPED OBJECT
            // =====================================================

            if (root.ValueKind ==
                JsonValueKind.Object)
            {
                foreach (string propertyName
                         in possiblePropertyNames)
                {
                    if (TryGetPropertyIgnoreCase(
                            root,
                            propertyName,
                            out JsonElement property))
                    {
                        // Normal array
                        if (property.ValueKind ==
                            JsonValueKind.Array)
                        {
                            return
                                JsonSerializer.Deserialize<List<T>>(
                                    property.GetRawText(),
                                    JsonOptions)

                                ?? new List<T>();
                        }


                        // $values wrapper
                        if (property.ValueKind ==
                            JsonValueKind.Object
                            &&
                            TryGetPropertyIgnoreCase(
                                property,
                                "$values",
                                out JsonElement wrappedValues)
                            &&
                            wrappedValues.ValueKind ==
                            JsonValueKind.Array)
                        {
                            return
                                JsonSerializer.Deserialize<List<T>>(
                                    wrappedValues.GetRawText(),
                                    JsonOptions)

                                ?? new List<T>();
                        }
                    }
                }


                // =================================================
                // ROOT $values
                // =================================================

                if (TryGetPropertyIgnoreCase(
                        root,
                        "$values",
                        out JsonElement rootValues)
                    &&
                    rootValues.ValueKind ==
                    JsonValueKind.Array)
                {
                    return
                        JsonSerializer.Deserialize<List<T>>(
                            rootValues.GetRawText(),
                            JsonOptions)

                        ?? new List<T>();
                }


                // =================================================
                // FALLBACK - FIRST ARRAY
                // =================================================

                foreach (JsonProperty property
                         in root.EnumerateObject())
                {
                    if (property.Value.ValueKind ==
                        JsonValueKind.Array)
                    {
                        return
                            JsonSerializer.Deserialize<List<T>>(
                                property.Value.GetRawText(),
                                JsonOptions)

                            ?? new List<T>();
                    }
                }
            }


            throw new JsonException(
                $"The API response could not be converted to " +
                $"{typeof(T).Name} records.");
        }


        // =========================================================
        // CASE-INSENSITIVE PROPERTY SEARCH
        // =========================================================

        private static bool TryGetPropertyIgnoreCase(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            foreach (JsonProperty property
                     in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    value =
                        property.Value;

                    return true;
                }
            }


            value =
                default;

            return false;
        }


        // =========================================================
        // DASHBOARD CALCULATIONS
        // =========================================================

        private void CalculateDashboard(
            string role)
        {
            int totalRecords =
                _records.Count;


            decimal businessValue =
                _records.Sum(
                    record =>
                        record.BusinessValue);


            int completed =
                CountStatus(
                    "Completed");


            int inProgress =
                CountStatus(
                    "In Progress");


            int pending =
                CountStatus(
                    "Pending");


            int onHold =
                CountStatus(
                    "On Hold");


            // =====================================================
            // ACTIVE EMPLOYEES
            // =====================================================

            int activeEmployees;


            if (string.Equals(
                    role,
                    "Administrator",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Administrator has access to the Employees endpoint,
                // therefore this is the true number of active accounts.

                activeEmployees =
                    _employees.Count(
                        employee =>
                            employee.IsActive);
            }
            else
            {
                // Manager does not have permission to read the
                // Administrator-only Employees endpoint.
                //
                // For the management dashboard we therefore show
                // active contributors represented in company records.

                activeEmployees =
                    _records

                        .Where(
                            record =>
                                record.EmployeeAccountId > 0)

                        .Select(
                            record =>
                                record.EmployeeAccountId)

                        .Distinct()

                        .Count();
            }


            // =====================================================
            // KPI CARDS
            // =====================================================

            TotalRecordsLabel.Text =
                totalRecords.ToString(
                    "N0");


            ActiveEmployeesLabel.Text =
                activeEmployees.ToString(
                    "N0");


            BusinessValueLabel.Text =
                $"R {businessValue:N2}";


            CompletedRecordsLabel.Text =
                completed.ToString(
                    "N0");


            // =====================================================
            // WORK STATUS
            // =====================================================

            StatusCompletedLabel.Text =
                completed.ToString(
                    "N0");


            StatusInProgressLabel.Text =
                inProgress.ToString(
                    "N0");


            StatusPendingLabel.Text =
                pending.ToString(
                    "N0");


            StatusOnHoldLabel.Text =
                onHold.ToString(
                    "N0");


            // =====================================================
            // DEPARTMENT ACTIVITY
            // =====================================================

            SalesActivityLabel.Text =
                CountDepartment(
                    "Sales")
                .ToString("N0");


            ITActivityLabel.Text =
                CountDepartment(
                    "Information Technology")
                .ToString("N0");


            FinanceActivityLabel.Text =
                CountDepartment(
                    "Finance")
                .ToString("N0");


            HRActivityLabel.Text =
                CountDepartment(
                    "Human Resources")
                .ToString("N0");


            OperationsActivityLabel.Text =
                CountDepartment(
                    "Operations")
                .ToString("N0");


            CustomerServiceActivityLabel.Text =
                CountDepartment(
                    "Customer Service")
                .ToString("N0");
        }


        // =========================================================
        // COUNT STATUS
        // =========================================================

        private int CountStatus(
            string status)
        {
            return
                _records.Count(
                    record =>
                        string.Equals(
                            record.Status?.Trim(),
                            status,
                            StringComparison.OrdinalIgnoreCase));
        }


        // =========================================================
        // COUNT DEPARTMENT
        // =========================================================

        private int CountDepartment(
            string department)
        {
            return
                _records.Count(
                    record =>
                        string.Equals(
                            record.Department?.Trim(),
                            department,
                            StringComparison.OrdinalIgnoreCase));
        }


        // =========================================================
        // RECENT RECORDS
        // =========================================================

        private void LoadRecentRecords()
        {
            List<DashboardDailyRecordDto> recentRecords =
                _records

                    .OrderByDescending(
                        record =>
                            record.SubmittedAt)

                    .Take(10)

                    .ToList();


            RecentRecordsCollectionView.ItemsSource =
                recentRecords;


            RecentRecordsCountLabel.Text =
                recentRecords.Count == 1
                    ? "1 record"
                    : $"{recentRecords.Count} records";
        }


        // =========================================================
        // RESET DASHBOARD
        // =========================================================

        private void ResetDashboard()
        {
            TotalRecordsLabel.Text =
                "0";

            ActiveEmployeesLabel.Text =
                "0";

            BusinessValueLabel.Text =
                "R 0.00";

            CompletedRecordsLabel.Text =
                "0";


            StatusCompletedLabel.Text =
                "0";

            StatusInProgressLabel.Text =
                "0";

            StatusPendingLabel.Text =
                "0";

            StatusOnHoldLabel.Text =
                "0";


            SalesActivityLabel.Text =
                "0";

            ITActivityLabel.Text =
                "0";

            FinanceActivityLabel.Text =
                "0";

            HRActivityLabel.Text =
                "0";

            OperationsActivityLabel.Text =
                "0";

            CustomerServiceActivityLabel.Text =
                "0";


            RecentRecordsCollectionView.ItemsSource =
                null;

            RecentRecordsCountLabel.Text =
                "0 records";
        }


        // =========================================================
        // REFRESH
        // =========================================================

        private async void OnRefreshClicked(
            object sender,
            EventArgs e)
        {
            await LoadDashboardAsync();
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
        // ERROR
        // =========================================================

        private void ShowError(
            string message)
        {
            ErrorLabel.Text =
                message;

            ErrorBorder.IsVisible =
                true;
        }


        private void HideError()
        {
            ErrorLabel.Text =
                string.Empty;

            ErrorBorder.IsVisible =
                false;
        }


        // =========================================================
        // READ API ERROR
        // =========================================================

        private static async Task<string>
            ReadErrorMessageAsync(
                HttpResponseMessage response)
        {
            try
            {
                string json =
                    await response.Content
                        .ReadAsStringAsync();


                if (string.IsNullOrWhiteSpace(
                        json))
                {
                    return string.Empty;
                }


                using JsonDocument document =
                    JsonDocument.Parse(
                        json);

                JsonElement root =
                    document.RootElement;


                if (root.ValueKind ==
                    JsonValueKind.Object)
                {
                    if (TryGetPropertyIgnoreCase(
                            root,
                            "message",
                            out JsonElement message))
                    {
                        return
                            message.GetString()
                            ?? string.Empty;
                    }


                    if (TryGetPropertyIgnoreCase(
                            root,
                            "title",
                            out JsonElement title))
                    {
                        return
                            title.GetString()
                            ?? string.Empty;
                    }
                }


                return json;
            }
            catch
            {
                return string.Empty;
            }
        }


        // =========================================================
        // NAVIGATION
        // =========================================================

        private void OnOverviewClicked(
            object sender,
            EventArgs e)
        {
            // Already on dashboard.
        }


        private async void OnSalesClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PushAsync(
                new SalesPage());
        }


        private async void OnCustomersClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PushAsync(
                new CustomersPage());
        }


        private async void OnProductsClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PushAsync(
                new ProductsPage());
        }


        private async void OnDepartmentsClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PushAsync(
                new RegionsPage());
        }


        private async void OnEmployeeRecordsClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PushAsync(
                new DataImportPage());
        }


        private async void OnEmployeeManagementClicked(
            object sender,
            EventArgs e)
        {
            // EmployeeManagementPage itself and the API remain
            // Administrator protected.
            //
            // A Manager may reach the page but cannot retrieve or
            // modify employee accounts.

            await Navigation.PushAsync(
                new EmployeeManagementPage());
        }


        private async void OnForecastingClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PushAsync(
                new ForecastingPage());
        }


        private async void OnReportsClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PushAsync(
                new ReportsPage());
        }


        private async void OnSettingsClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PushAsync(
                new SettingsPage());
        }


        // =========================================================
        // LOGOUT
        // =========================================================

        private async void OnLogoutClicked(
            object sender,
            EventArgs e)
        {
            bool confirm =
                await DisplayAlert(
                    "Log Out",
                    "Are you sure you want to log out?",
                    "Log Out",
                    "Cancel");


            if (!confirm)
            {
                return;
            }


            try
            {
                _httpClient
                    .DefaultRequestHeaders
                    .Authorization = null;


                SecureStorage.Default.Remove(
                    "auth_token");

                SecureStorage.Default.Remove(
                    "employee_account_id");

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
                    "employee_department_id");

                SecureStorage.Default.Remove(
                    "employee_department_code");

                SecureStorage.Default.Remove(
                    "employee_role");
            }
            catch
            {
                // Continue to login even if secure storage cleanup fails.
            }


            if (Application.Current != null)
            {
                Application.Current.MainPage =
                    new NavigationPage(
                        new MainPage());
            }
        }
    }


    // =============================================================
    // EMPLOYEE DTO
    // =============================================================

    public class DashboardEmployeeDto
    {
        public long AccountId { get; set; }


        public string EmployeeId { get; set; } =
            string.Empty;


        public string FirstName { get; set; } =
            string.Empty;


        public string LastName { get; set; } =
            string.Empty;


        public string CompanyEmail { get; set; } =
            string.Empty;


        public int DepartmentId { get; set; }


        public string Department { get; set; } =
            string.Empty;


        public string DepartmentCode { get; set; } =
            string.Empty;


        public string JobTitle { get; set; } =
            string.Empty;


        public string Role { get; set; } =
            string.Empty;


        public bool IsActive { get; set; }


        public DateTime DateCreated { get; set; }


        public string FullName =>
            $"{FirstName} {LastName}".Trim();
    }


    // =============================================================
    // DAILY RECORD DTO
    // =============================================================

    public class DashboardDailyRecordDto
    {
        public long RecordId { get; set; }


        // Employee information is nested in the API response.

        public DashboardRecordEmployeeDto? Employee { get; set; }


        public long EmployeeAccountId =>
            Employee?.AccountId ?? 0;


        public string EmployeeId =>
            Employee?.EmployeeId?.Trim()
            ?? string.Empty;


        public string EmployeeName
        {
            get
            {
                if (Employee == null)
                {
                    return string.Empty;
                }


                return
                    $"{Employee.FirstName} {Employee.LastName}"
                        .Trim();
            }
        }


        public string Department =>
            Employee?.Department?.Trim()
            ?? string.Empty;


        public string DepartmentCode =>
            Employee?.DepartmentCode?.Trim()
            ?? string.Empty;


        public DateTime RecordDate { get; set; }


        public string ActivityType { get; set; } =
            string.Empty;


        public string Reference { get; set; } =
            string.Empty;


        public int Quantity { get; set; }


        public decimal BusinessValue { get; set; }


        public int SecondaryMetric { get; set; }


        public string Status { get; set; } =
            string.Empty;


        public string BusinessImpact { get; set; } =
            string.Empty;


        public string Notes { get; set; } =
            string.Empty;


        public DateTime SubmittedAt { get; set; }


        // =========================================================
        // DISPLAY PROPERTIES
        // =========================================================

        public string DateDisplay =>
            RecordDate.ToString(
                "dd MMM yyyy");


        public string BusinessValueDisplay =>
            $"R {BusinessValue:N2}";
    }


    // =============================================================
    // NESTED EMPLOYEE DTO
    // =============================================================

    public class DashboardRecordEmployeeDto
    {
        public long AccountId { get; set; }


        public string EmployeeId { get; set; } =
            string.Empty;


        public string FirstName { get; set; } =
            string.Empty;


        public string LastName { get; set; } =
            string.Empty;


        public string Department { get; set; } =
            string.Empty;


        public string DepartmentCode { get; set; } =
            string.Empty;
    }
}