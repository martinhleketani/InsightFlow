using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InsightFlow
{
    public partial class ProductsPage : ContentPage
    {
        private const string ApiBaseUrl =
            "https://localhost:7212/";

        private readonly HttpClient _httpClient;

        private readonly List<ProductDailyRecordDto>
            _productRecords = new();


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ProductsPage()
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

            await LoadProductActivityAsync();
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
        // LOAD DATA
        // =========================================================

        private async Task LoadProductActivityAsync()
        {
            try
            {
                SetLoading(true);

                HideError();

                _productRecords.Clear();

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
                        "Administrator or Manager access is required to view product analytics.");

                    return;
                }


                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await ReadErrorMessageAsync(
                            response);

                    ShowError(
                        string.IsNullOrWhiteSpace(error)
                            ? "Unable to load product/service activity."
                            : error);

                    return;
                }


                // =================================================
                // FLEXIBLE JSON READING
                // =================================================

                string json =
                    await response.Content
                        .ReadAsStringAsync();

                List<ProductDailyRecordDto> records =
                    ParseDailyRecords(json);


                /*
                 * V1 PRODUCT/SERVICE RULE:
                 *
                 * DailyRecord does not currently contain a
                 * dedicated ProductId or ProductName field.
                 *
                 * A non-empty Reference is therefore used as
                 * the recorded product/service reference.
                 */

                List<ProductDailyRecordDto>
                    productRecords =
                        records
                            .Where(
                                IsProductRelatedRecord)
                            .ToList();


                _productRecords.AddRange(
                    productRecords);


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
                    $"Unable to load product analytics: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }


        // =========================================================
        // FLEXIBLE DAILY RECORD JSON PARSER
        // =========================================================

        private static List<ProductDailyRecordDto>
            ParseDailyRecords(
                string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<ProductDailyRecordDto>();
            }


            using JsonDocument document =
                JsonDocument.Parse(json);


            JsonElement root =
                document.RootElement;


            JsonSerializerOptions options =
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };


            // -----------------------------------------------------
            // FORMAT 1:
            //
            // [
            //     { record... },
            //     { record... }
            // ]
            // -----------------------------------------------------

            if (root.ValueKind ==
                JsonValueKind.Array)
            {
                return
                    JsonSerializer.Deserialize<
                        List<ProductDailyRecordDto>>(
                        root.GetRawText(),
                        options)

                    ?? new List<ProductDailyRecordDto>();
            }


            // -----------------------------------------------------
            // FORMAT 2:
            //
            // {
            //     "records": [...]
            // }
            //
            // or:
            //
            // {
            //     "data": [...]
            // }
            // -----------------------------------------------------

            if (root.ValueKind ==
                JsonValueKind.Object)
            {
                string[] possibleProperties =
                {
                    "records",
                    "data",
                    "items",
                    "dailyRecords",
                    "results"
                };


                foreach (string propertyName
                         in possibleProperties)
                {
                    if (TryGetPropertyIgnoreCase(
                            root,
                            propertyName,
                            out JsonElement property)
                        &&
                        property.ValueKind ==
                        JsonValueKind.Array)
                    {
                        return
                            JsonSerializer.Deserialize<
                                List<ProductDailyRecordDto>>(
                                property.GetRawText(),
                                options)

                            ?? new List<ProductDailyRecordDto>();
                    }
                }


                // -------------------------------------------------
                // FORMAT 3:
                //
                // {
                //     "success": true,
                //     "records": [...]
                // }
                //
                // Nested object support
                // -------------------------------------------------

                foreach (JsonProperty jsonProperty
                         in root.EnumerateObject())
                {
                    if (jsonProperty.Value.ValueKind ==
                        JsonValueKind.Array)
                    {
                        try
                        {
                            List<ProductDailyRecordDto>? records =
                                JsonSerializer.Deserialize<
                                    List<ProductDailyRecordDto>>(
                                    jsonProperty.Value.GetRawText(),
                                    options);

                            if (records != null)
                            {
                                return records;
                            }
                        }
                        catch
                        {
                            // Continue looking for another array.
                        }
                    }


                    if (jsonProperty.Value.ValueKind ==
                        JsonValueKind.Object)
                    {
                        JsonElement nested =
                            jsonProperty.Value;


                        foreach (string propertyName
                                 in possibleProperties)
                        {
                            if (TryGetPropertyIgnoreCase(
                                    nested,
                                    propertyName,
                                    out JsonElement nestedArray)
                                &&
                                nestedArray.ValueKind ==
                                JsonValueKind.Array)
                            {
                                return
                                    JsonSerializer.Deserialize<
                                        List<ProductDailyRecordDto>>(
                                        nestedArray.GetRawText(),
                                        options)

                                    ?? new List<ProductDailyRecordDto>();
                            }
                        }
                    }
                }
            }


            throw new JsonException(
                "DailyRecords API response format was not recognized.");
        }


        // =========================================================
        // CASE-INSENSITIVE JSON PROPERTY LOOKUP
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
        // PRODUCT / SERVICE RECORD RULE
        // =========================================================

        private static bool IsProductRelatedRecord(
            ProductDailyRecordDto record)
        {
            /*
             * Until InsightFlow has a dedicated Products table,
             * we only include records that have a Reference.
             *
             * The Reference can identify a product, service,
             * order, item, project or another business reference.
             */

            return !string.IsNullOrWhiteSpace(
                record.Reference);
        }


        // =========================================================
        // CALCULATE METRICS
        // =========================================================

        private void CalculateMetrics()
        {
            int totalActivities =
                _productRecords.Count;


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
                _productRecords.Sum(
                    record =>
                        record.BusinessValue);


            long totalQuantity =
                _productRecords.Sum(
                    record =>
                        (long)record.Quantity);


            long secondaryOutput =
                _productRecords.Sum(
                    record =>
                        (long)record.SecondaryMetric);


            int references =
                _productRecords

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


            int contributors =
                _productRecords

                    .Where(
                        record =>
                            record.EmployeeAccountId > 0)

                    .Select(
                        record =>
                            record.EmployeeAccountId)

                    .Distinct()

                    .Count();


            decimal averageValue =
                totalActivities == 0
                    ? 0
                    : businessValue /
                      totalActivities;


            double completionRate =
                totalActivities == 0
                    ? 0
                    : (double)completed /
                      totalActivities * 100;


            // =====================================================
            // MAIN KPI
            // =====================================================

            TotalActivitiesLabel.Text =
                totalActivities.ToString("N0");


            ProductReferencesLabel.Text =
                references.ToString("N0");


            TotalQuantityLabel.Text =
                totalQuantity.ToString("N0");


            BusinessValueLabel.Text =
                $"R {businessValue:N2}";


            // =====================================================
            // SECONDARY KPI
            // =====================================================

            CompletedLabel.Text =
                completed.ToString("N0");


            CompletionRateLabel.Text =
                $"{completionRate:N1}%";


            ContributorsLabel.Text =
                contributors.ToString("N0");


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
            // RECENT ACTIVITY
            // =====================================================

            DateTime today =
                DateTime.Today;


            DateTime sevenDaysAgo =
                today.AddDays(-6);


            DateTime thirtyDaysAgo =
                today.AddDays(-29);


            Last7DaysLabel.Text =
                _productRecords.Count(
                    record =>
                        record.RecordDate.Date >=
                        sevenDaysAgo)
                .ToString("N0");


            Last30DaysLabel.Text =
                _productRecords.Count(
                    record =>
                        record.RecordDate.Date >=
                        thirtyDaysAgo)
                .ToString("N0");


            AverageValueLabel.Text =
                $"R {averageValue:N2}";


            SecondaryMetricLabel.Text =
                secondaryOutput.ToString("N0");
        }


        // =========================================================
        // STATUS COUNT
        // =========================================================

        private int CountStatus(
            string status)
        {
            return _productRecords.Count(
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
            List<ProductActivityBreakdownItem>
                breakdown =
                    _productRecords

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
                                new ProductActivityBreakdownItem
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


            IEnumerable<ProductDailyRecordDto>
                result =
                    _productRecords;


            if (!string.IsNullOrWhiteSpace(
                    search))
            {
                result =
                    result.Where(
                        record =>

                            ContainsText(
                                record.Reference,
                                search)

                            ||

                            ContainsText(
                                record.ActivityType,
                                search)

                            ||

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


            List<ProductDailyRecordDto>
                displayRecords =
                    result

                        .OrderByDescending(
                            record =>
                                record.SubmittedAt)

                        .ThenByDescending(
                            record =>
                                record.RecordDate)

                        .ToList();


            ProductRecordsCollectionView.ItemsSource =
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


            ProductReferencesLabel.Text =
                "0";


            TotalQuantityLabel.Text =
                "0";


            BusinessValueLabel.Text =
                "R 0.00";


            CompletedLabel.Text =
                "0";


            CompletionRateLabel.Text =
                "0%";


            ContributorsLabel.Text =
                "0";


            StatusCompletedLabel.Text =
                "0";


            StatusInProgressLabel.Text =
                "0";


            StatusPendingLabel.Text =
                "0";


            StatusOnHoldLabel.Text =
                "0";


            Last7DaysLabel.Text =
                "0";


            Last30DaysLabel.Text =
                "0";


            AverageValueLabel.Text =
                "R 0.00";


            SecondaryMetricLabel.Text =
                "0";


            RecordCountLabel.Text =
                "0 records";


            ActivityBreakdownCollectionView.ItemsSource =
                null;


            ProductRecordsCollectionView.ItemsSource =
                null;


            LastUpdatedLabel.Text =
                "Waiting for product activity";
        }


        // =========================================================
        // REFRESH
        // =========================================================

        private async void OnRefreshClicked(
            object sender,
            EventArgs e)
        {
            await LoadProductActivityAsync();
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
    // PRODUCT DAILY RECORD DTO
    // =============================================================

    public class ProductDailyRecordDto
    {
        public long RecordId { get; set; }


        // =========================================================
        // NESTED EMPLOYEE RETURNED BY API
        // =========================================================

        public ProductEmployeeDto? Employee { get; set; }


        // =========================================================
        // RESOLVED EMPLOYEE INFORMATION
        // =========================================================

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


                string firstName =
                    Employee.FirstName?.Trim()
                    ?? string.Empty;


                string lastName =
                    Employee.LastName?.Trim()
                    ?? string.Empty;


                return
                    $"{firstName} {lastName}".Trim();
            }
        }


        public string Department =>
            Employee?.Department?.Trim()
            ?? string.Empty;


        public string DepartmentCode =>
            Employee?.DepartmentCode?.Trim()
            ?? string.Empty;


        // =========================================================
        // RECORD DATA
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
                ? "No reference"
                : Reference;


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

    public class ProductEmployeeDto
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
    // ACTIVITY BREAKDOWN
    // =============================================================

    public class ProductActivityBreakdownItem
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