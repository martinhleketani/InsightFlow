using System.Net;
using System.Text.Json;
using InsightFlow.Models;
using InsightFlow.Services;

namespace InsightFlow
{
    public partial class EmployeeDashboardPage : ContentPage
    {
        private readonly EmployeeAccount currentEmployee;
        private readonly ApiService _apiService;

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public EmployeeDashboardPage(EmployeeAccount employee)
        {
            InitializeComponent();

            currentEmployee = employee
                ?? throw new ArgumentNullException(nameof(employee));

            _apiService = new ApiService();

            LoadEmployeeInformation();
        }


        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            LoadEmployeeInformation();

            await LoadEmployeeDashboardAsync();
        }


        // =========================================================
        // EMPLOYEE INFORMATION
        // =========================================================

        private void LoadEmployeeInformation()
        {
            EmployeeNameLabel.Text =
                currentEmployee.FullName;

            EmployeeIdLabel.Text =
                currentEmployee.EmployeeId;

            DepartmentLabel.Text =
                currentEmployee.Department;

            EmployeePositionLabel.Text =
                currentEmployee.JobTitle;


            // Employee initial
            if (!string.IsNullOrWhiteSpace(
                    currentEmployee.FirstName))
            {
                EmployeeInitialLabel.Text =
                    currentEmployee.FirstName
                        .Substring(0, 1)
                        .ToUpper();
            }
            else
            {
                EmployeeInitialLabel.Text = "E";
            }


            // Current date
            CurrentDateLabel.Text =
                DateTime.Now.ToString(
                    "dddd, dd MMMM yyyy");
        }


        // =========================================================
        // LOAD DASHBOARD FROM API / MYSQL
        // =========================================================

        private async Task LoadEmployeeDashboardAsync()
        {
            try
            {
                bool authenticated =
                    await _apiService
                        .RestoreAuthorizationTokenAsync();

                if (!authenticated)
                {
                    SetEmptyDashboard();

                    await DisplayAlert(
                        "Session",
                        "Your login session could not be restored. Please sign in again.",
                        "OK");

                    return;
                }


                HttpResponseMessage response =
                    await _apiService.Client.GetAsync(
                        "api/DailyRecords/my");


                // =================================================
                // UNAUTHORIZED
                // =================================================

                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    SetEmptyDashboard();

                    await DisplayAlert(
                        "Session Expired",
                        "Please sign in again.",
                        "OK");

                    return;
                }


                // =================================================
                // API ERROR
                // =================================================

                if (!response.IsSuccessStatusCode)
                {
                    SetEmptyDashboard();

                    string error =
                        await ReadErrorMessageAsync(
                            response);

                    await DisplayAlert(
                        "Dashboard Error",
                        string.IsNullOrWhiteSpace(error)
                            ? "The employee dashboard could not be loaded."
                            : error,
                        "OK");

                    return;
                }


                // =================================================
                // READ RAW JSON
                // =================================================

                string json =
                    await response.Content
                        .ReadAsStringAsync();


                List<ApiDailyRecord> employeeRecords =
                    ParseDailyRecords(json);


                // =================================================
                // TOTAL ENTRIES
                // =================================================

                TotalEntriesLabel.Text =
                    employeeRecords.Count.ToString();


                // =================================================
                // DAYS REPORTED
                // =================================================

                int daysReported =
                    employeeRecords
                        .Select(
                            record =>
                                record.RecordDate.Date)
                        .Distinct()
                        .Count();


                DaysReportedLabel.Text =
                    daysReported.ToString();


                // =================================================
                // TODAY'S SUBMISSION STATUS
                // =================================================

                bool submittedToday =
                    employeeRecords.Any(
                        record =>
                            record.RecordDate.Date ==
                            DateTime.Today);


                DailyEntryStatusLabel.Text =
                    submittedToday
                        ? "SUBMITTED"
                        : "NOT SUBMITTED";
            }
            catch (HttpRequestException)
            {
                SetEmptyDashboard();

                await DisplayAlert(
                    "Connection Error",
                    "InsightFlow could not connect to the API. Make sure the API is running.",
                    "OK");
            }
            catch (TaskCanceledException)
            {
                SetEmptyDashboard();

                await DisplayAlert(
                    "Connection Timeout",
                    "The InsightFlow API took too long to respond.",
                    "OK");
            }
            catch (JsonException)
            {
                SetEmptyDashboard();

                await DisplayAlert(
                    "Dashboard Error",
                    "The API returned data in an unexpected format.",
                    "OK");
            }
            catch (Exception ex)
            {
                SetEmptyDashboard();

                await DisplayAlert(
                    "Dashboard Error",
                    $"The employee dashboard could not be loaded: {ex.Message}",
                    "OK");
            }
        }


        // =========================================================
        // ROBUST DAILY RECORD JSON PARSER
        // =========================================================

        private static List<ApiDailyRecord>
            ParseDailyRecords(
                string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<ApiDailyRecord>();
            }


            using JsonDocument document =
                JsonDocument.Parse(json);


            JsonElement root =
                document.RootElement;


            // =====================================================
            // FORMAT 1
            // [ {...}, {...} ]
            // =====================================================

            if (root.ValueKind ==
                JsonValueKind.Array)
            {
                return DeserializeArray(root);
            }


            if (root.ValueKind !=
                JsonValueKind.Object)
            {
                return new List<ApiDailyRecord>();
            }


            // =====================================================
            // FORMAT 2
            // { "$values": [...] }
            // =====================================================

            if (TryGetArrayProperty(
                    root,
                    "$values",
                    out JsonElement valuesArray))
            {
                return DeserializeArray(
                    valuesArray);
            }


            // =====================================================
            // FORMAT 3
            // { "records": [...] }
            // { "data": [...] }
            // { "items": [...] }
            // =====================================================

            string[] propertyNames =
            {
                "records",
                "data",
                "items"
            };


            foreach (string propertyName
                     in propertyNames)
            {
                if (!TryGetPropertyIgnoreCase(
                        root,
                        propertyName,
                        out JsonElement property))
                {
                    continue;
                }


                if (property.ValueKind ==
                    JsonValueKind.Array)
                {
                    return DeserializeArray(
                        property);
                }


                // =============================================
                // FORMAT 4
                //
                // {
                //     "records":
                //     {
                //         "$values": [...]
                //     }
                // }
                // =============================================

                if (property.ValueKind ==
                    JsonValueKind.Object &&
                    TryGetArrayProperty(
                        property,
                        "$values",
                        out JsonElement nestedValues))
                {
                    return DeserializeArray(
                        nestedValues);
                }
            }


            return new List<ApiDailyRecord>();
        }


        // =========================================================
        // DESERIALIZE ARRAY
        // =========================================================

        private static List<ApiDailyRecord>
            DeserializeArray(
                JsonElement array)
        {
            try
            {
                return JsonSerializer.Deserialize<
                           List<ApiDailyRecord>>(
                           array.GetRawText(),
                           JsonOptions)
                       ?? new List<ApiDailyRecord>();
            }
            catch
            {
                return new List<ApiDailyRecord>();
            }
        }


        // =========================================================
        // GET ARRAY PROPERTY
        // =========================================================

        private static bool TryGetArrayProperty(
            JsonElement element,
            string propertyName,
            out JsonElement array)
        {
            array = default;


            if (!TryGetPropertyIgnoreCase(
                    element,
                    propertyName,
                    out JsonElement property))
            {
                return false;
            }


            if (property.ValueKind !=
                JsonValueKind.Array)
            {
                return false;
            }


            array = property;

            return true;
        }


        // =========================================================
        // CASE-INSENSITIVE JSON PROPERTY LOOKUP
        // =========================================================

        private static bool TryGetPropertyIgnoreCase(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            value = default;


            if (element.ValueKind !=
                JsonValueKind.Object)
            {
                return false;
            }


            foreach (JsonProperty property
                     in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;

                    return true;
                }
            }


            return false;
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


                if (string.IsNullOrWhiteSpace(json))
                {
                    return string.Empty;
                }


                using JsonDocument document =
                    JsonDocument.Parse(json);


                if (TryGetPropertyIgnoreCase(
                        document.RootElement,
                        "message",
                        out JsonElement message))
                {
                    return message.GetString()
                        ?? string.Empty;
                }


                if (TryGetPropertyIgnoreCase(
                        document.RootElement,
                        "title",
                        out JsonElement title))
                {
                    return title.GetString()
                        ?? string.Empty;
                }


                return json;
            }
            catch
            {
                return string.Empty;
            }
        }


        // =========================================================
        // EMPTY DASHBOARD
        // =========================================================

        private void SetEmptyDashboard()
        {
            TotalEntriesLabel.Text =
                "0";

            DaysReportedLabel.Text =
                "0";

            DailyEntryStatusLabel.Text =
                "NOT SUBMITTED";
        }


        // =========================================================
        // DAILY ENTRY
        // =========================================================

        private async void OnRecordDailyDataClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PushAsync(
                new DailyEntryPage(
                    currentEmployee));
        }


        // =========================================================
        // MY ENTRIES
        // =========================================================

        private async void OnMyEntriesClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PushAsync(
                new MyEntriesPage());
        }


        // =========================================================
        // MY PERFORMANCE
        // =========================================================

        private async void OnMyPerformanceClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PushAsync(
                new MyPerformancePage());
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
                    "Logout",
                    "Are you sure you want to sign out of InsightFlow?",
                    "Logout",
                    "Cancel");


            if (!logout)
            {
                return;
            }


            // Remove JWT and employee session information
            _apiService.ClearSession();


            // Return to login page
            Application.Current!.MainPage =
                new NavigationPage(
                    new MainPage());
        }


        // =========================================================
        // API DAILY RECORD DTO
        // =========================================================

        private class ApiDailyRecord
        {
            public long RecordId { get; set; }

            public long EmployeeAccountId { get; set; }

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
        }
    }
}