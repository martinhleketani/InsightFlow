using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InsightFlow
{
    public partial class CustomersPage : ContentPage
    {
        private const string ApiBaseUrl =
            "https://localhost:7212/";

        private readonly HttpClient _httpClient;

        private readonly List<CustomerDailyRecordDto>
            _customerRecords = new();


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CustomersPage()
        {
            InitializeComponent();

            _httpClient =
                new HttpClient
                {
                    BaseAddress =
                        new Uri(ApiBaseUrl)
                };

            ResetDashboard();
        }


        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadCustomerActivityAsync();
        }


        // =========================================================
        // AUTHENTICATION
        // =========================================================

        private async Task<bool>
            ConfigureAuthenticationAsync()
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


        // =========================================================
        // LOAD CUSTOMER ACTIVITY
        // =========================================================

        private async Task LoadCustomerActivityAsync()
        {
            try
            {
                SetLoading(true);

                HideError();

                _customerRecords.Clear();

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


                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    ShowError(
                        "Your session has expired. Please sign in again.");

                    return;
                }


                if (response.StatusCode ==
                    HttpStatusCode.Forbidden)
                {
                    ShowError(
                        "Administrator or Manager access is required to view customer activity.");

                    return;
                }


                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await ReadErrorMessageAsync(
                            response);

                    ShowError(
                        string.IsNullOrWhiteSpace(error)
                            ? "Unable to load customer activity."
                            : error);

                    return;
                }


                // =================================================
                // READ API RESPONSE
                // =================================================

                string json =
                    await response.Content
                        .ReadAsStringAsync();

                List<CustomerDailyRecordDto>
                    companyRecords =
                        ParseDailyRecords(json);


                // =================================================
                // CUSTOMER-RELATED RECORDS
                // =================================================

                List<CustomerDailyRecordDto>
                    customerRecords =
                        companyRecords
                            .Where(
                                IsCustomerRelatedRecord)
                            .ToList();


                _customerRecords.AddRange(
                    customerRecords);


                CalculateMetrics();

                BuildActivityBreakdown();

                ApplySearchFilter();


                LastUpdatedLabel.Text =
                    $"Last updated: {DateTime.Now:dd MMM yyyy, HH:mm}";
            }
            catch (HttpRequestException)
            {
                ShowError(
                    "Unable to connect to the InsightFlow API. Make sure the API is running at https://localhost:7212/.");
            }
            catch (JsonException ex)
            {
                ShowError(
                    $"The API returned data in an unexpected format: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError(
                    $"Unable to load customer activity: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }


        // =========================================================
        // FLEXIBLE DAILY RECORD JSON PARSER
        // =========================================================

        private static List<CustomerDailyRecordDto>
            ParseDailyRecords(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<CustomerDailyRecordDto>();
            }


            JsonSerializerOptions options =
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };


            using JsonDocument document =
                JsonDocument.Parse(json);

            JsonElement root =
                document.RootElement;


            // =====================================================
            // FORMAT 1:
            // [ { record }, { record } ]
            // =====================================================

            if (root.ValueKind ==
                JsonValueKind.Array)
            {
                return
                    JsonSerializer.Deserialize<
                        List<CustomerDailyRecordDto>>(
                            root.GetRawText(),
                            options)

                    ?? new List<CustomerDailyRecordDto>();
            }


            // =====================================================
            // FORMAT 2:
            // { "records": [...] }
            // { "data": [...] }
            // etc.
            // =====================================================

            if (root.ValueKind ==
                JsonValueKind.Object)
            {
                string[] possibleArrayNames =
                {
                    "records",
                    "dailyRecords",
                    "data",
                    "items",
                    "results"
                };


                foreach (string propertyName
                         in possibleArrayNames)
                {
                    if (TryGetPropertyIgnoreCase(
                            root,
                            propertyName,
                            out JsonElement arrayElement))
                    {
                        // Normal array
                        if (arrayElement.ValueKind ==
                            JsonValueKind.Array)
                        {
                            return
                                JsonSerializer.Deserialize<
                                    List<CustomerDailyRecordDto>>(
                                        arrayElement.GetRawText(),
                                        options)

                                ?? new List<CustomerDailyRecordDto>();
                        }


                        // Support:
                        // "records": { "$values": [...] }
                        if (arrayElement.ValueKind ==
                            JsonValueKind.Object
                            &&
                            TryGetPropertyIgnoreCase(
                                arrayElement,
                                "$values",
                                out JsonElement wrappedValues)
                            &&
                            wrappedValues.ValueKind ==
                            JsonValueKind.Array)
                        {
                            return
                                JsonSerializer.Deserialize<
                                    List<CustomerDailyRecordDto>>(
                                        wrappedValues.GetRawText(),
                                        options)

                                ?? new List<CustomerDailyRecordDto>();
                        }
                    }
                }


                // =================================================
                // ROOT $values SUPPORT
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
                        JsonSerializer.Deserialize<
                            List<CustomerDailyRecordDto>>(
                                rootValues.GetRawText(),
                                options)

                        ?? new List<CustomerDailyRecordDto>();
                }


                // =================================================
                // FALLBACK:
                // Find first array in response object.
                // =================================================

                foreach (JsonProperty property
                         in root.EnumerateObject())
                {
                    if (property.Value.ValueKind ==
                        JsonValueKind.Array)
                    {
                        return
                            JsonSerializer.Deserialize<
                                List<CustomerDailyRecordDto>>(
                                    property.Value.GetRawText(),
                                    options)

                            ?? new List<CustomerDailyRecordDto>();
                    }
                }
            }


            throw new JsonException(
                "The DailyRecords response does not contain a record array.");
        }


        // =========================================================
        // CASE-INSENSITIVE JSON PROPERTY SEARCH
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
        // CUSTOMER RECORD RULE
        // =========================================================

        private static bool IsCustomerRelatedRecord(
            CustomerDailyRecordDto record)
        {
            bool sales =
                IsSalesRecord(record);

            bool customerService =
                IsCustomerServiceRecord(record);

            return sales ||
                   customerService;
        }


        // =========================================================
        // CALCULATE METRICS
        // =========================================================

        private void CalculateMetrics()
        {
            int totalActivities =
                _customerRecords.Count;


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


            decimal businessValue =
                _customerRecords.Sum(
                    record =>
                        record.BusinessValue);


            long quantity =
                _customerRecords.Sum(
                    record =>
                        (long)record.Quantity);


            int contributors =
                _customerRecords

                    .Where(
                        record =>
                            record.EmployeeAccountId > 0)

                    .Select(
                        record =>
                            record.EmployeeAccountId)

                    .Distinct()

                    .Count();


            int references =
                _customerRecords

                    .Where(
                        record =>
                            !string.IsNullOrWhiteSpace(
                                record.Reference))

                    .Select(
                        record =>
                            record.Reference.Trim())

                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)

                    .Count();


            double completionRate =
                totalActivities == 0
                    ? 0
                    : (double)completed /
                      totalActivities * 100;


            // =====================================================
            // MAIN CARDS
            // =====================================================

            TotalActivitiesLabel.Text =
                totalActivities.ToString("N0");


            CustomerReferencesLabel.Text =
                references.ToString("N0");


            BusinessValueLabel.Text =
                $"R {businessValue:N2}";


            CompletedLabel.Text =
                completed.ToString("N0");


            // =====================================================
            // SECONDARY CARDS
            // =====================================================

            ContributorsLabel.Text =
                contributors.ToString("N0");


            CompletionRateLabel.Text =
                $"{completionRate:N1}%";


            TotalQuantityLabel.Text =
                quantity.ToString("N0");


            // =====================================================
            // STATUS
            // =====================================================

            StatusCompletedLabel.Text =
                completed.ToString("N0");


            StatusInProgressLabel.Text =
                inProgress.ToString("N0");


            StatusPendingLabel.Text =
                pending.ToString("N0");


            StatusOnHoldLabel.Text =
                onHold.ToString("N0");


            // =====================================================
            // SOURCE DEPARTMENTS
            // =====================================================

            SalesSourceLabel.Text =
                _customerRecords.Count(
                    IsSalesRecord)
                .ToString("N0");


            CustomerServiceSourceLabel.Text =
                _customerRecords.Count(
                    IsCustomerServiceRecord)
                .ToString("N0");


            // =====================================================
            // RECENT ACTIVITY
            // =====================================================

            DateTime today =
                DateTime.Today;


            DateTime sevenDaysAgo =
                today.AddDays(-6);


            DateTime thirtyDaysAgo =
                today.AddDays(-29);


            Last7DaysLabel.Text =
                _customerRecords.Count(
                    record =>
                        record.RecordDate.Date >=
                        sevenDaysAgo
                        &&
                        record.RecordDate.Date <=
                        today)
                .ToString("N0");


            Last30DaysLabel.Text =
                _customerRecords.Count(
                    record =>
                        record.RecordDate.Date >=
                        thirtyDaysAgo
                        &&
                        record.RecordDate.Date <=
                        today)
                .ToString("N0");
        }


        // =========================================================
        // SALES RECORD
        // =========================================================

        private static bool IsSalesRecord(
            CustomerDailyRecordDto record)
        {
            return
                string.Equals(
                    record.Department?.Trim(),
                    "Sales",
                    StringComparison.OrdinalIgnoreCase)

                ||

                string.Equals(
                    record.DepartmentCode?.Trim(),
                    "IF-SAL",
                    StringComparison.OrdinalIgnoreCase)

                ||

                string.Equals(
                    record.DepartmentCode?.Trim(),
                    "SAL",
                    StringComparison.OrdinalIgnoreCase);
        }


        // =========================================================
        // CUSTOMER SERVICE RECORD
        // =========================================================

        private static bool IsCustomerServiceRecord(
            CustomerDailyRecordDto record)
        {
            return
                string.Equals(
                    record.Department?.Trim(),
                    "Customer Service",
                    StringComparison.OrdinalIgnoreCase)

                ||

                string.Equals(
                    record.DepartmentCode?.Trim(),
                    "IF-CS",
                    StringComparison.OrdinalIgnoreCase)

                ||

                string.Equals(
                    record.DepartmentCode?.Trim(),
                    "CS",
                    StringComparison.OrdinalIgnoreCase);
        }


        // =========================================================
        // STATUS COUNT
        // =========================================================

        private int CountStatus(
            string status)
        {
            return _customerRecords.Count(
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
            List<CustomerActivityBreakdownItem>
                breakdown =
                    _customerRecords

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
                                new CustomerActivityBreakdownItem
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
        // SEARCH
        // =========================================================

        private void OnSearchTextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }


        private void ApplySearchFilter()
        {
            string search =
                SearchEntry.Text?.Trim()
                ?? string.Empty;


            IEnumerable<CustomerDailyRecordDto>
                result =
                    _customerRecords;


            if (!string.IsNullOrWhiteSpace(
                    search))
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
                                record.Department,
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
                                record.Status,
                                search)

                            ||

                            ContainsText(
                                record.BusinessImpact,
                                search)

                            ||

                            ContainsText(
                                record.Notes,
                                search));
            }


            List<CustomerDailyRecordDto>
                displayRecords =
                    result

                        .OrderByDescending(
                            record =>
                                record.SubmittedAt)

                        .ThenByDescending(
                            record =>
                                record.RecordDate)

                        .ToList();


            CustomerRecordsCollectionView.ItemsSource =
                displayRecords;


            RecordCountLabel.Text =
                displayRecords.Count == 1
                    ? "1 record"
                    : $"{displayRecords.Count} records";
        }


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
        // RESET
        // =========================================================

        private void ResetDashboard()
        {
            TotalActivitiesLabel.Text =
                "0";

            CustomerReferencesLabel.Text =
                "0";

            BusinessValueLabel.Text =
                "R 0.00";

            CompletedLabel.Text =
                "0";

            ContributorsLabel.Text =
                "0";

            CompletionRateLabel.Text =
                "0%";

            TotalQuantityLabel.Text =
                "0";

            StatusCompletedLabel.Text =
                "0";

            StatusInProgressLabel.Text =
                "0";

            StatusPendingLabel.Text =
                "0";

            StatusOnHoldLabel.Text =
                "0";

            SalesSourceLabel.Text =
                "0";

            CustomerServiceSourceLabel.Text =
                "0";

            Last7DaysLabel.Text =
                "0";

            Last30DaysLabel.Text =
                "0";

            RecordCountLabel.Text =
                "0 records";

            ActivityBreakdownCollectionView.ItemsSource =
                null;

            CustomerRecordsCollectionView.ItemsSource =
                null;

            LastUpdatedLabel.Text =
                "Waiting for customer activity";
        }


        // =========================================================
        // REFRESH
        // =========================================================

        private async void OnRefreshClicked(
            object sender,
            EventArgs e)
        {
            await LoadCustomerActivityAsync();
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
        // API ERROR
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


                if (document.RootElement.ValueKind ==
                    JsonValueKind.Object)
                {
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
    // DAILY RECORD DTO
    // =============================================================

    public class CustomerDailyRecordDto
    {
        public long RecordId { get; set; }


        // =========================================================
        // NESTED EMPLOYEE OBJECT FROM DAILYRECORDS API
        // =========================================================

        public CustomerRecordEmployeeDto? Employee { get; set; }


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


        // =========================================================
        // DAILY RECORD FIELDS
        // =========================================================

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
                ? "No reference recorded"
                : $"Reference: {Reference}";


        public string NotesDisplay =>
            string.IsNullOrWhiteSpace(
                Notes)
                ? "No additional notes"
                : Notes;


        public string EmployeeDetailsDisplay
        {
            get
            {
                string employee =
                    string.IsNullOrWhiteSpace(
                        EmployeeId)
                        ? "No employee ID"
                        : EmployeeId;


                if (!string.IsNullOrWhiteSpace(
                        Department))
                {
                    return
                        $"{employee} • {Department}";
                }


                return employee;
            }
        }
    }


    // =============================================================
    // NESTED EMPLOYEE DTO
    // =============================================================

    public class CustomerRecordEmployeeDto
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


    // =============================================================
    // ACTIVITY BREAKDOWN ITEM
    // =============================================================

    public class CustomerActivityBreakdownItem
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