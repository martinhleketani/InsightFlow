using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InsightFlow
{
    public partial class MyPerformancePage : ContentPage
    {
        private const string ApiBaseUrl =
            "https://localhost:7212/";

        private readonly HttpClient _httpClient;

        private readonly List<PerformanceDailyRecordDto> _records =
            new();

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public MyPerformancePage()
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
        }


        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await ConfigureAuthenticationAsync();

            await LoadEmployeeInformationAsync();

            await LoadPerformanceAsync();
        }


        // =========================================================
        // JWT
        // =========================================================

        private async Task ConfigureAuthenticationAsync()
        {
            string? token =
                await SecureStorage.Default.GetAsync(
                    "auth_token");

            if (string.IsNullOrWhiteSpace(token))
            {
                _httpClient
                    .DefaultRequestHeaders
                    .Authorization = null;

                return;
            }

            _httpClient
                .DefaultRequestHeaders
                .Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);
        }


        // =========================================================
        // EMPLOYEE INFORMATION
        // =========================================================

        private async Task LoadEmployeeInformationAsync()
        {
            try
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

                string? department =
                    await SecureStorage.Default.GetAsync(
                        "employee_department");

                string fullName =
                    $"{firstName} {lastName}".Trim();


                EmployeeNameLabel.Text =
                    string.IsNullOrWhiteSpace(fullName)
                        ? "Employee"
                        : fullName;


                string details =
                    string.Empty;


                if (!string.IsNullOrWhiteSpace(
                        employeeId))
                {
                    details = employeeId;
                }


                if (!string.IsNullOrWhiteSpace(
                        department))
                {
                    if (!string.IsNullOrWhiteSpace(
                            details))
                    {
                        details += " • ";
                    }

                    details += department;
                }


                EmployeeDetailsLabel.Text =
                    string.IsNullOrWhiteSpace(details)
                        ? "Employee performance"
                        : details;
            }
            catch
            {
                EmployeeNameLabel.Text =
                    "Employee";

                EmployeeDetailsLabel.Text =
                    "Employee performance";
            }
        }


        // =========================================================
        // LOAD PERFORMANCE DATA
        // =========================================================

        private async Task LoadPerformanceAsync()
        {
            try
            {
                SetLoading(true);

                await ConfigureAuthenticationAsync();


                HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        "api/DailyRecords/my");


                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    await DisplayAlert(
                        "Session Expired",
                        "Please sign in again.",
                        "OK");

                    return;
                }


                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await ReadErrorMessageAsync(
                            response);


                    await DisplayAlert(
                        "Unable to Load Performance",
                        string.IsNullOrWhiteSpace(error)
                            ? "Your performance data could not be loaded."
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


                List<PerformanceDailyRecordDto> records =
                    ParseDailyRecords(json);


                _records.Clear();

                _records.AddRange(records);


                CalculatePerformance();
            }
            catch (HttpRequestException)
            {
                await DisplayAlert(
                    "Connection Error",
                    "Unable to connect to the InsightFlow API. Make sure the API is running.",
                    "OK");
            }
            catch (TaskCanceledException)
            {
                await DisplayAlert(
                    "Connection Timeout",
                    "The InsightFlow API took too long to respond.",
                    "OK");
            }
            catch (JsonException)
            {
                await DisplayAlert(
                    "Data Error",
                    "The API returned data in an unexpected format.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Error",
                    $"Unable to calculate your performance: {ex.Message}",
                    "OK");
            }
            finally
            {
                SetLoading(false);
            }
        }


        // =========================================================
        // ROBUST DAILY RECORD JSON PARSER
        // =========================================================

        private static List<PerformanceDailyRecordDto>
            ParseDailyRecords(
                string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<PerformanceDailyRecordDto>();
            }


            using JsonDocument document =
                JsonDocument.Parse(json);


            JsonElement root =
                document.RootElement;


            // Plain JSON array

            if (root.ValueKind ==
                JsonValueKind.Array)
            {
                return DeserializeArray(root);
            }


            if (root.ValueKind !=
                JsonValueKind.Object)
            {
                return new List<PerformanceDailyRecordDto>();
            }


            // { "$values": [...] }

            if (TryGetArrayProperty(
                    root,
                    "$values",
                    out JsonElement valuesArray))
            {
                return DeserializeArray(
                    valuesArray);
            }


            // { "records": [...] }
            // { "data": [...] }
            // { "items": [...] }

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


                // {
                //   "records": {
                //       "$values": [...]
                //   }
                // }

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


            return new List<PerformanceDailyRecordDto>();
        }


        // =========================================================
        // DESERIALIZE ARRAY
        // =========================================================

        private static List<PerformanceDailyRecordDto>
            DeserializeArray(
                JsonElement array)
        {
            try
            {
                return JsonSerializer.Deserialize<
                           List<PerformanceDailyRecordDto>>(
                           array.GetRawText(),
                           JsonOptions)
                       ?? new List<PerformanceDailyRecordDto>();
            }
            catch
            {
                return new List<PerformanceDailyRecordDto>();
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
        // CASE-INSENSITIVE PROPERTY LOOKUP
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
        // CALCULATE PERFORMANCE
        // =========================================================

        private void CalculatePerformance()
        {
            int totalActivities =
                _records.Count;


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


            decimal totalBusinessValue =
                _records.Sum(
                    record =>
                        record.BusinessValue);


            int totalQuantity =
                _records.Sum(
                    record =>
                        record.Quantity);


            int totalSecondaryMetric =
                _records.Sum(
                    record =>
                        record.SecondaryMetric);


            double completionRate =
                totalActivities == 0
                    ? 0
                    : (double)completed /
                      totalActivities * 100;


            decimal averageBusinessValue =
                totalActivities == 0
                    ? 0
                    : totalBusinessValue /
                      totalActivities;


            // MAIN METRICS

            TotalActivitiesLabel.Text =
                totalActivities.ToString();


            CompletedActivitiesLabel.Text =
                completed.ToString();


            CompletionRateLabel.Text =
                $"{completionRate:N1}%";


            BusinessValueLabel.Text =
                $"R {totalBusinessValue:N2}";


            TotalQuantityLabel.Text =
                totalQuantity.ToString("N0");


            AverageBusinessValueLabel.Text =
                $"R {averageBusinessValue:N2}";


            SecondaryMetricLabel.Text =
                totalSecondaryMetric.ToString("N0");


            // STATUS

            CompletedStatusLabel.Text =
                completed.ToString();


            InProgressStatusLabel.Text =
                inProgress.ToString();


            PendingStatusLabel.Text =
                pending.ToString();


            OnHoldStatusLabel.Text =
                onHold.ToString();


            CalculateRecentActivity();

            BuildActivityBreakdown();
        }


        // =========================================================
        // STATUS COUNT
        // =========================================================

        private int CountStatus(
            string status)
        {
            return _records.Count(
                record =>
                    string.Equals(
                        record.Status,
                        status,
                        StringComparison.OrdinalIgnoreCase));
        }


        // =========================================================
        // RECENT ACTIVITY
        // =========================================================

        private void CalculateRecentActivity()
        {
            DateTime today =
                DateTime.Today;


            DateTime sevenDaysAgo =
                today.AddDays(-6);


            DateTime thirtyDaysAgo =
                today.AddDays(-29);


            List<PerformanceDailyRecordDto>
                lastSevenDays =
                    _records
                        .Where(
                            record =>
                                record.RecordDate.Date >=
                                sevenDaysAgo)
                        .ToList();


            List<PerformanceDailyRecordDto>
                lastThirtyDays =
                    _records
                        .Where(
                            record =>
                                record.RecordDate.Date >=
                                thirtyDaysAgo)
                        .ToList();


            Last7DaysLabel.Text =
                lastSevenDays.Count.ToString();


            Last30DaysLabel.Text =
                lastThirtyDays.Count.ToString();


            decimal thirtyDayValue =
                lastThirtyDays.Sum(
                    record =>
                        record.BusinessValue);


            Last30DaysValueLabel.Text =
                $"R {thirtyDayValue:N2}";


            PerformanceDailyRecordDto? latest =
                _records
                    .OrderByDescending(
                        record =>
                            record.SubmittedAt)
                    .FirstOrDefault();


            LatestSubmissionLabel.Text =
                latest == null
                    ? "No records"
                    : latest.SubmittedAt
                        .ToLocalTime()
                        .ToString(
                            "dd MMM yyyy");
        }


        // =========================================================
        // ACTIVITY BREAKDOWN
        // =========================================================

        private void BuildActivityBreakdown()
        {
            List<ActivityPerformanceItem>
                activityBreakdown =
                    _records

                        .Where(
                            record =>
                                !string.IsNullOrWhiteSpace(
                                    record.ActivityType))

                        .GroupBy(
                            record =>
                                record.ActivityType,
                            StringComparer.OrdinalIgnoreCase)

                        .Select(
                            group =>
                                new ActivityPerformanceItem
                                {
                                    ActivityType =
                                        group.Key,

                                    Count =
                                        group.Count(),

                                    BusinessValue =
                                        group.Sum(
                                            record =>
                                                record.BusinessValue)
                                })

                        .OrderByDescending(
                            item =>
                                item.Count)

                        .ThenBy(
                            item =>
                                item.ActivityType)

                        .ToList();


            ActivityBreakdownCollectionView.ItemsSource =
                activityBreakdown;
        }


        // =========================================================
        // REFRESH
        // =========================================================

        private async void OnRefreshClicked(
            object sender,
            EventArgs e)
        {
            await LoadEmployeeInformationAsync();

            await LoadPerformanceAsync();
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
        // BACK
        // =========================================================

        private async void OnBackClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }


    // =============================================================
    // DAILY RECORD DTO
    // =============================================================

    public class PerformanceDailyRecordDto
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


    // =============================================================
    // ACTIVITY BREAKDOWN ITEM
    // =============================================================

    public class ActivityPerformanceItem
    {
        public string ActivityType { get; set; } =
            string.Empty;

        public int Count { get; set; }

        public decimal BusinessValue { get; set; }


        public string CountDisplay =>
            Count == 1
                ? "1 record"
                : $"{Count} records";


        public string ValueDisplay =>
            $"R {BusinessValue:N2}";
    }
}