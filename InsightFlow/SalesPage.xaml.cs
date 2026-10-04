using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InsightFlow
{
    public partial class SalesPage : ContentPage
    {
        private const string ApiBaseUrl =
            "https://localhost:7212/";

        private readonly HttpClient _httpClient;

        private readonly List<SalesDailyRecordDto> _salesRecords =
            new();

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public SalesPage()
        {
            InitializeComponent();

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            ResetDashboard();
        }

        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadSalesAsync();
        }

        // =========================================================
        // AUTHENTICATION
        // =========================================================

        private async Task<bool> ConfigureAuthenticationAsync()
        {
            try
            {
                string? token =
                    await SecureStorage.Default.GetAsync(
                        "auth_token");

                if (string.IsNullOrWhiteSpace(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        null;

                    ShowError(
                        "You are not signed in. Please log in again.");

                    return false;
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        token);

                return true;
            }
            catch
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    null;

                ShowError(
                    "Unable to restore your login session.");

                return false;
            }
        }

        // =========================================================
        // LOAD SALES DATA
        // =========================================================

        private async Task LoadSalesAsync()
        {
            try
            {
                SetLoading(true);

                HideError();

                // Clear old data before loading fresh data.
                _salesRecords.Clear();

                ResetDashboard();

                bool authenticated =
                    await ConfigureAuthenticationAsync();

                if (!authenticated)
                {
                    return;
                }

                using HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        "api/DailyRecords");

                // -------------------------------------------------
                // UNAUTHORIZED
                // -------------------------------------------------

                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    ShowError(
                        "Your session has expired. Please sign in again.");

                    return;
                }

                // -------------------------------------------------
                // FORBIDDEN
                // -------------------------------------------------

                if (response.StatusCode ==
                    HttpStatusCode.Forbidden)
                {
                    ShowError(
                        "Administrator or Manager access is required " +
                        "to view company Sales records.");

                    return;
                }

                // -------------------------------------------------
                // OTHER API ERROR
                // -------------------------------------------------

                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await ReadErrorMessageAsync(response);

                    ShowError(
                        string.IsNullOrWhiteSpace(error)
                            ? "Unable to load Sales records."
                            : error);

                    return;
                }

                // -------------------------------------------------
                // READ JSON
                // -------------------------------------------------

                string json =
                    await response.Content.ReadAsStringAsync();

                List<SalesDailyRecordDto> companyRecords =
                    DeserializeRecordList(json);

                // -------------------------------------------------
                // ONLY SALES DEPARTMENT RECORDS
                // -------------------------------------------------

                List<SalesDailyRecordDto> salesRecords =
                    companyRecords
                        .Where(IsSalesRecord)
                        .ToList();

                _salesRecords.AddRange(
                    salesRecords);

                // -------------------------------------------------
                // CALCULATE PAGE DATA
                // -------------------------------------------------

                CalculateSalesMetrics();

                BuildActivityBreakdown();

                ApplySearchFilter();

                LastUpdatedLabel.Text =
                    $"Last updated: {DateTime.Now:dd MMM yyyy, HH:mm}";
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
                    $"The API returned data in an unexpected format. " +
                    $"{ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError(
                    $"Unable to load Sales analytics: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        // =========================================================
        // DESERIALIZE DAILY RECORDS
        // =========================================================

        private static List<SalesDailyRecordDto>
            DeserializeRecordList(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<SalesDailyRecordDto>();
            }

            using JsonDocument document =
                JsonDocument.Parse(json);

            JsonElement root =
                document.RootElement;

            // -----------------------------------------------------
            // FORMAT 1
            //
            // [
            //     { ... },
            //     { ... }
            // ]
            // -----------------------------------------------------

            if (root.ValueKind ==
                JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<
                           List<SalesDailyRecordDto>>(
                           root.GetRawText(),
                           JsonOptions)
                       ?? new List<SalesDailyRecordDto>();
            }

            // -----------------------------------------------------
            // FORMAT 2
            //
            // {
            //     "records": [...]
            // }
            //
            // OR
            //
            // {
            //     "dailyRecords": [...]
            // }
            //
            // OR
            //
            // {
            //     "data": [...]
            // }
            // -----------------------------------------------------

            if (root.ValueKind ==
                JsonValueKind.Object)
            {
                string[] possiblePropertyNames =
                {
                    "records",
                    "dailyRecords",
                    "data",
                    "items",
                    "results"
                };

                foreach (string propertyName
                         in possiblePropertyNames)
                {
                    if (TryGetPropertyIgnoreCase(
                            root,
                            propertyName,
                            out JsonElement property))
                    {
                        if (property.ValueKind ==
                            JsonValueKind.Array)
                        {
                            return JsonSerializer.Deserialize<
                                       List<SalesDailyRecordDto>>(
                                       property.GetRawText(),
                                       JsonOptions)
                                   ?? new List<SalesDailyRecordDto>();
                        }
                    }
                }

                // -------------------------------------------------
                // FALLBACK
                //
                // If API uses another property name,
                // use first array found.
                // -------------------------------------------------

                foreach (JsonProperty property
                         in root.EnumerateObject())
                {
                    if (property.Value.ValueKind ==
                        JsonValueKind.Array)
                    {
                        return JsonSerializer.Deserialize<
                                   List<SalesDailyRecordDto>>(
                                   property.Value.GetRawText(),
                                   JsonOptions)
                               ?? new List<SalesDailyRecordDto>();
                    }
                }
            }

            throw new JsonException(
                "DailyRecords response does not contain a readable record list.");
        }

        // =========================================================
        // CASE INSENSITIVE JSON PROPERTY
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

            value = default;

            return false;
        }

        // =========================================================
        // SALES DEPARTMENT FILTER
        // =========================================================

        private static bool IsSalesRecord(
            SalesDailyRecordDto record)
        {
            return
                string.Equals(
                    record.Department?.Trim(),
                    "Sales",
                    StringComparison.OrdinalIgnoreCase)

                ||

                string.Equals(
                    record.DepartmentCode?.Trim(),
                    "SAL",
                    StringComparison.OrdinalIgnoreCase)

                ||

                string.Equals(
                    record.DepartmentCode?.Trim(),
                    "IF-SAL",
                    StringComparison.OrdinalIgnoreCase);
        }

        // =========================================================
        // CALCULATE SALES METRICS
        // =========================================================

        private void CalculateSalesMetrics()
        {
            int totalActivities =
                _salesRecords.Count;

            decimal totalBusinessValue =
                _salesRecords.Sum(
                    record =>
                        record.BusinessValue);

            long totalQuantity =
                _salesRecords.Sum(
                    record =>
                        (long)record.Quantity);

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

            decimal averageValue =
                totalActivities == 0
                    ? 0
                    : totalBusinessValue /
                      totalActivities;

            double completionRate =
                totalActivities == 0
                    ? 0
                    : (double)completed /
                      totalActivities *
                      100;

            int contributors =
                _salesRecords
                    .Where(
                        record =>
                            record.EmployeeAccountId > 0)
                    .Select(
                        record =>
                            record.EmployeeAccountId)
                    .Distinct()
                    .Count();

            // =====================================================
            // MAIN KPI CARDS
            // =====================================================

            TotalActivitiesLabel.Text =
                totalActivities.ToString("N0");

            TotalBusinessValueLabel.Text =
                $"R {totalBusinessValue:N2}";

            TotalQuantityLabel.Text =
                totalQuantity.ToString("N0");

            CompletedActivitiesLabel.Text =
                completed.ToString("N0");

            // =====================================================
            // SECONDARY METRICS
            // =====================================================

            AverageValueLabel.Text =
                $"R {averageValue:N2}";

            CompletionRateLabel.Text =
                $"{completionRate:N1}%";

            ContributorsLabel.Text =
                contributors.ToString("N0");

            // =====================================================
            // STATUS BREAKDOWN
            // =====================================================

            CompletedStatusLabel.Text =
                completed.ToString("N0");

            InProgressStatusLabel.Text =
                inProgress.ToString("N0");

            PendingStatusLabel.Text =
                pending.ToString("N0");

            OnHoldStatusLabel.Text =
                onHold.ToString("N0");
        }

        // =========================================================
        // COUNT STATUS
        // =========================================================

        private int CountStatus(
            string status)
        {
            return _salesRecords.Count(
                record =>
                    string.Equals(
                        record.Status?.Trim(),
                        status,
                        StringComparison.OrdinalIgnoreCase));
        }

        // =========================================================
        // ACTIVITY BREAKDOWN
        // =========================================================

        private void BuildActivityBreakdown()
        {
            List<SalesActivityBreakdownItem> breakdown =
                _salesRecords
                    .Where(
                        record =>
                            !string.IsNullOrWhiteSpace(
                                record.ActivityType))
                    .GroupBy(
                        record =>
                            record.ActivityType.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .Select(
                        group =>
                            new SalesActivityBreakdownItem
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
                breakdown;
        }

        // =========================================================
        // SEARCH EVENT
        // =========================================================

        private void OnSearchTextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        // =========================================================
        // APPLY SEARCH FILTER
        // =========================================================

        private void ApplySearchFilter()
        {
            string search =
                SearchEntry.Text?.Trim()
                ?? string.Empty;

            IEnumerable<SalesDailyRecordDto> result =
                _salesRecords;

            if (!string.IsNullOrWhiteSpace(search))
            {
                result =
                    result.Where(
                        record =>
                            ContainsText(
                                record.EmployeeName,
                                search)

                            ||

                            ContainsText(
                                record.EmployeeId,
                                search)

                            ||

                            ContainsText(
                                record.ActivityType,
                                search)

                            ||

                            ContainsText(
                                record.Reference,
                                search)

                            ||

                            ContainsText(
                                record.Notes,
                                search)

                            ||

                            ContainsText(
                                record.Status,
                                search));
            }

            List<SalesDailyRecordDto> displayRecords =
                result
                    .OrderByDescending(
                        record =>
                            record.SubmittedAt)
                    .ThenByDescending(
                        record =>
                            record.RecordDate)
                    .ToList();

            SalesRecordsCollectionView.ItemsSource =
                displayRecords;

            RecordCountLabel.Text =
                displayRecords.Count == 1
                    ? "1 record"
                    : $"{displayRecords.Count} records";
        }

        // =========================================================
        // TEXT SEARCH HELPER
        // =========================================================

        private static bool ContainsText(
            string? value,
            string search)
        {
            return
                !string.IsNullOrWhiteSpace(value)
                &&
                value.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase);
        }

        // =========================================================
        // RESET DASHBOARD
        // =========================================================

        private void ResetDashboard()
        {
            TotalActivitiesLabel.Text =
                "0";

            TotalBusinessValueLabel.Text =
                "R 0.00";

            TotalQuantityLabel.Text =
                "0";

            CompletedActivitiesLabel.Text =
                "0";

            AverageValueLabel.Text =
                "R 0.00";

            CompletionRateLabel.Text =
                "0%";

            ContributorsLabel.Text =
                "0";

            CompletedStatusLabel.Text =
                "0";

            InProgressStatusLabel.Text =
                "0";

            PendingStatusLabel.Text =
                "0";

            OnHoldStatusLabel.Text =
                "0";

            ActivityBreakdownCollectionView.ItemsSource =
                null;

            SalesRecordsCollectionView.ItemsSource =
                null;

            RecordCountLabel.Text =
                "0 records";

            LastUpdatedLabel.Text =
                "Waiting for sales data";
        }

        // =========================================================
        // REFRESH
        // =========================================================

        private async void OnRefreshClicked(
            object sender,
            EventArgs e)
        {
            await LoadSalesAsync();
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
        // ERROR DISPLAY
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
        // READ API ERROR MESSAGE
        // =========================================================

        private static async Task<string>
            ReadErrorMessageAsync(
                HttpResponseMessage response)
        {
            try
            {
                string content =
                    await response.Content
                        .ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(
                        content))
                {
                    return string.Empty;
                }

                using JsonDocument document =
                    JsonDocument.Parse(
                        content);

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

                return content;
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
    // SALES DAILY RECORD DTO
    // =============================================================

    public class SalesDailyRecordDto
    {
        public long RecordId { get; set; }

        public long EmployeeAccountId { get; set; }

        public string EmployeeId { get; set; } =
            string.Empty;

        public string EmployeeName { get; set; } =
            string.Empty;

        public string Department { get; set; } =
            string.Empty;

        public string DepartmentCode { get; set; } =
            string.Empty;

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

        public string QuantityDisplay =>
            $"Qty: {Quantity:N0}";

        public string ReferenceDisplay =>
            string.IsNullOrWhiteSpace(
                Reference)
                ? "No reference"
                : $"Reference: {Reference}";

        public string NotesDisplay =>
            string.IsNullOrWhiteSpace(
                Notes)
                ? "No additional notes"
                : Notes;
    }

    // =============================================================
    // ACTIVITY BREAKDOWN
    // =============================================================

    public class SalesActivityBreakdownItem
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