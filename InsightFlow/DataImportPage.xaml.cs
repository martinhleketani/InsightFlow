using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InsightFlow
{
    public partial class DataImportPage : ContentPage
    {
        private const string ApiBaseUrl =
            "https://localhost:7212/";

        private readonly HttpClient _httpClient;

        private readonly List<EmployeeRecordDto>
            _records = new();

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public DataImportPage()
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

            DepartmentFilterPicker.SelectedIndex = 0;
            StatusFilterPicker.SelectedIndex = 0;

            ResetPage();
        }


        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadRecordsAsync();
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
        // LOAD RECORDS
        // =========================================================

        private async Task LoadRecordsAsync()
        {
            try
            {
                SetLoading(true);

                HideError();

                _records.Clear();

                ResetMetrics();


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
                        "Administrator or Manager access is required to view employee records.");

                    return;
                }


                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await ReadErrorMessageAsync(
                            response);

                    ShowError(
                        string.IsNullOrWhiteSpace(error)
                            ? "Unable to load employee records."
                            : error);

                    return;
                }


                // =================================================
                // READ RAW JSON
                // =================================================

                string json =
                    await response.Content
                        .ReadAsStringAsync();


                // =================================================
                // PARSE API RESPONSE
                // =================================================

                List<EmployeeRecordDto> records =
                    ParseEmployeeRecords(json);


                _records.AddRange(
                    records);


                CalculateMetrics();

                ApplyFilters();


                LastUpdatedLabel.Text =
                    $"Last updated: {DateTime.Now:dd MMM yyyy, HH:mm}";
            }
            catch (HttpRequestException)
            {
                ShowError(
                    "Unable to connect to the InsightFlow API. Make sure the API is running at https://localhost:7212/.");
            }
            catch (TaskCanceledException)
            {
                ShowError(
                    "The request to the InsightFlow API timed out.");
            }
            catch (JsonException ex)
            {
                ShowError(
                    $"The API returned employee records in an unexpected format: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError(
                    $"Unable to load employee records: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }


        // =========================================================
        // PARSE EMPLOYEE RECORDS
        // =========================================================

        private static List<EmployeeRecordDto>
            ParseEmployeeRecords(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<EmployeeRecordDto>();
            }


            using JsonDocument document =
                JsonDocument.Parse(json);


            JsonElement root =
                document.RootElement;


            // =====================================================
            // FORMAT 1:
            // [ {...}, {...} ]
            // =====================================================

            if (root.ValueKind ==
                JsonValueKind.Array)
            {
                return DeserializeRecordArray(
                    root);
            }


            if (root.ValueKind ==
                JsonValueKind.Object)
            {
                // =================================================
                // FORMAT 2:
                // { "$values": [...] }
                // =================================================

                if (TryGetArrayProperty(
                        root,
                        "$values",
                        out JsonElement values))
                {
                    return DeserializeRecordArray(
                        values);
                }


                // =================================================
                // FORMAT 3:
                // {
                //   "success": true,
                //   "count": 1,
                //   "records": [...]
                // }
                // =================================================

                if (TryGetArrayProperty(
                        root,
                        "records",
                        out JsonElement records))
                {
                    return DeserializeRecordArray(
                        records);
                }


                // =================================================
                // FORMAT 4:
                // { "data": [...] }
                // =================================================

                if (TryGetArrayProperty(
                        root,
                        "data",
                        out JsonElement data))
                {
                    return DeserializeRecordArray(
                        data);
                }


                // =================================================
                // FORMAT 5:
                // { "items": [...] }
                // =================================================

                if (TryGetArrayProperty(
                        root,
                        "items",
                        out JsonElement items))
                {
                    return DeserializeRecordArray(
                        items);
                }


                // =================================================
                // FORMAT 6:
                // {
                //   "records": {
                //       "$values": [...]
                //   }
                // }
                // =================================================

                if (TryGetPropertyIgnoreCase(
                        root,
                        "records",
                        out JsonElement recordsObject)

                    &&

                    recordsObject.ValueKind ==
                    JsonValueKind.Object

                    &&

                    TryGetArrayProperty(
                        recordsObject,
                        "$values",
                        out JsonElement nestedRecords))
                {
                    return DeserializeRecordArray(
                        nestedRecords);
                }


                // =================================================
                // FORMAT 7:
                // {
                //   "data": {
                //       "$values": [...]
                //   }
                // }
                // =================================================

                if (TryGetPropertyIgnoreCase(
                        root,
                        "data",
                        out JsonElement dataObject)

                    &&

                    dataObject.ValueKind ==
                    JsonValueKind.Object

                    &&

                    TryGetArrayProperty(
                        dataObject,
                        "$values",
                        out JsonElement nestedData))
                {
                    return DeserializeRecordArray(
                        nestedData);
                }


                // =================================================
                // FORMAT 8:
                // {
                //   "items": {
                //       "$values": [...]
                //   }
                // }
                // =================================================

                if (TryGetPropertyIgnoreCase(
                        root,
                        "items",
                        out JsonElement itemsObject)

                    &&

                    itemsObject.ValueKind ==
                    JsonValueKind.Object

                    &&

                    TryGetArrayProperty(
                        itemsObject,
                        "$values",
                        out JsonElement nestedItems))
                {
                    return DeserializeRecordArray(
                        nestedItems);
                }
            }


            throw new JsonException(
                "DailyRecords response did not contain a supported employee-record collection.");
        }


        // =========================================================
        // DESERIALIZE RECORD ARRAY
        // =========================================================

        private static List<EmployeeRecordDto>
            DeserializeRecordArray(
                JsonElement array)
        {
            if (array.ValueKind !=
                JsonValueKind.Array)
            {
                return new List<EmployeeRecordDto>();
            }


            List<EmployeeRecordDto>? records =
                JsonSerializer.Deserialize<
                    List<EmployeeRecordDto>>(
                        array.GetRawText(),
                        JsonOptions);


            return records ??
                   new List<EmployeeRecordDto>();
        }


        // =========================================================
        // GET ARRAY PROPERTY
        // =========================================================

        private static bool TryGetArrayProperty(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            if (TryGetPropertyIgnoreCase(
                    element,
                    propertyName,
                    out value))
            {
                return value.ValueKind ==
                       JsonValueKind.Array;
            }


            value = default;

            return false;
        }


        // =========================================================
        // GET PROPERTY - CASE INSENSITIVE
        // =========================================================

        private static bool TryGetPropertyIgnoreCase(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            if (element.ValueKind !=
                JsonValueKind.Object)
            {
                value = default;

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
                    value =
                        property.Value;

                    return true;
                }
            }


            value = default;

            return false;
        }


        // =========================================================
        // CALCULATE SUMMARY
        // =========================================================

        private void CalculateMetrics()
        {
            int total =
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


            // The API identifies the employee through
            // the nested employee object.
            int contributors =
                _records

                    .Where(
                        record =>
                            record.EmployeeAccountId > 0)

                    .Select(
                        record =>
                            record.EmployeeAccountId)

                    .Distinct()

                    .Count();


            decimal businessValue =
                _records.Sum(
                    record =>
                        record.BusinessValue);


            double completionRate =
                total == 0
                    ? 0
                    : (double)completed /
                      total * 100;


            TotalRecordsLabel.Text =
                total.ToString("N0");


            ContributorsLabel.Text =
                contributors.ToString("N0");


            CompletedRecordsLabel.Text =
                completed.ToString("N0");


            BusinessValueLabel.Text =
                $"R {businessValue:N2}";


            InProgressLabel.Text =
                inProgress.ToString("N0");


            PendingLabel.Text =
                pending.ToString("N0");


            OnHoldLabel.Text =
                onHold.ToString("N0");


            CompletionRateLabel.Text =
                $"{completionRate:N1}%";
        }


        // =========================================================
        // COUNT STATUS
        // =========================================================

        private int CountStatus(
            string status)
        {
            return _records.Count(
                record =>
                    string.Equals(
                        record.Status?.Trim(),
                        status,
                        StringComparison.OrdinalIgnoreCase));
        }


        // =========================================================
        // SEARCH
        // =========================================================

        private void OnSearchTextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            ApplyFilters();
        }


        // =========================================================
        // PICKER FILTER
        // =========================================================

        private void OnFilterChanged(
            object sender,
            EventArgs e)
        {
            ApplyFilters();
        }


        // =========================================================
        // APPLY FILTERS
        // =========================================================

        private void ApplyFilters()
        {
            string search =
                SearchEntry.Text?.Trim()
                ?? string.Empty;


            string department =
                DepartmentFilterPicker
                    .SelectedItem?
                    .ToString()
                ?? "All Departments";


            string status =
                StatusFilterPicker
                    .SelectedItem?
                    .ToString()
                ?? "All Statuses";


            IEnumerable<EmployeeRecordDto>
                result =
                    _records;


            // =====================================================
            // SEARCH FILTER
            // =====================================================

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
                                record.DepartmentCode,
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


            // =====================================================
            // DEPARTMENT FILTER
            // =====================================================

            if (!string.Equals(
                    department,
                    "All Departments",
                    StringComparison.OrdinalIgnoreCase))
            {
                result =
                    result.Where(
                        record =>
                            string.Equals(
                                record.Department.Trim(),
                                department,
                                StringComparison.OrdinalIgnoreCase));
            }


            // =====================================================
            // STATUS FILTER
            // =====================================================

            if (!string.Equals(
                    status,
                    "All Statuses",
                    StringComparison.OrdinalIgnoreCase))
            {
                result =
                    result.Where(
                        record =>
                            string.Equals(
                                record.Status?.Trim(),
                                status,
                                StringComparison.OrdinalIgnoreCase));
            }


            List<EmployeeRecordDto>
                filteredRecords =
                    result

                        .OrderByDescending(
                            record =>
                                record.SubmittedAt)

                        .ThenByDescending(
                            record =>
                                record.RecordDate)

                        .ToList();


            RecordsCollectionView.ItemsSource =
                filteredRecords;


            RecordCountLabel.Text =
                filteredRecords.Count == 1
                    ? "1 record"
                    : $"{filteredRecords.Count} records";


            BuildFilterSummary(
                filteredRecords.Count,
                department,
                status,
                search);
        }


        // =========================================================
        // FILTER SUMMARY
        // =========================================================

        private void BuildFilterSummary(
            int count,
            string department,
            string status,
            string search)
        {
            List<string> filters =
                new();


            if (!string.Equals(
                    department,
                    "All Departments",
                    StringComparison.OrdinalIgnoreCase))
            {
                filters.Add(
                    department);
            }


            if (!string.Equals(
                    status,
                    "All Statuses",
                    StringComparison.OrdinalIgnoreCase))
            {
                filters.Add(
                    status);
            }


            if (!string.IsNullOrWhiteSpace(
                    search))
            {
                filters.Add(
                    $"Search: \"{search}\"");
            }


            if (filters.Count == 0)
            {
                FilterSummaryLabel.Text =
                    "Showing all employee records";

                return;
            }


            FilterSummaryLabel.Text =
                $"Showing {count:N0} record(s) • " +
                string.Join(
                    " • ",
                    filters);
        }


        // =========================================================
        // CLEAR FILTERS
        // =========================================================

        private void OnClearFiltersClicked(
            object sender,
            EventArgs e)
        {
            SearchEntry.Text =
                string.Empty;

            DepartmentFilterPicker.SelectedIndex =
                0;

            StatusFilterPicker.SelectedIndex =
                0;

            ApplyFilters();
        }


        // =========================================================
        // CONTAINS TEXT
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
        // RESET PAGE
        // =========================================================

        private void ResetPage()
        {
            ResetMetrics();

            RecordsCollectionView.ItemsSource =
                null;

            RecordCountLabel.Text =
                "0 records";

            FilterSummaryLabel.Text =
                "Showing all employee records";

            LastUpdatedLabel.Text =
                "Waiting for employee records";
        }


        // =========================================================
        // RESET METRICS
        // =========================================================

        private void ResetMetrics()
        {
            TotalRecordsLabel.Text =
                "0";

            ContributorsLabel.Text =
                "0";

            CompletedRecordsLabel.Text =
                "0";

            BusinessValueLabel.Text =
                "R 0.00";

            InProgressLabel.Text =
                "0";

            PendingLabel.Text =
                "0";

            OnHoldLabel.Text =
                "0";

            CompletionRateLabel.Text =
                "0%";
        }


        // =========================================================
        // REFRESH
        // =========================================================

        private async void OnRefreshClicked(
            object sender,
            EventArgs e)
        {
            await LoadRecordsAsync();
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
        // API ERROR MESSAGE
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
    // EMPLOYEE RECORD DTO
    // =============================================================

    public class EmployeeRecordDto
    {
        public long RecordId { get; set; }


        // =========================================================
        // NESTED EMPLOYEE FROM API
        // =========================================================

        public EmployeeRecordEmployeeDto? Employee
        {
            get;
            set;
        }


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
        // EMPLOYEE VALUES RESOLVED FROM NESTED OBJECT
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
                    $"{firstName} {lastName}"
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
        // DISPLAY PROPERTIES
        // =========================================================

        public string EmployeeNameDisplay =>
            string.IsNullOrWhiteSpace(
                EmployeeName)
                ? "Unknown Employee"
                : EmployeeName;


        public string EmployeeIdDisplay =>
            string.IsNullOrWhiteSpace(
                EmployeeId)
                ? "No employee ID"
                : EmployeeId;


        public string DepartmentDisplay =>
            string.IsNullOrWhiteSpace(
                Department)
                ? "Unknown Department"
                : Department;


        public string ActivityTypeDisplay =>
            string.IsNullOrWhiteSpace(
                ActivityType)
                ? "No activity type"
                : ActivityType;


        public string ReferenceDisplay =>
            string.IsNullOrWhiteSpace(
                Reference)
                ? "No reference"
                : Reference;


        public string QuantityDisplay =>
            Quantity.ToString("N0");


        public string BusinessValueDisplay =>
            $"R {BusinessValue:N2}";


        public string StatusDisplay =>
            string.IsNullOrWhiteSpace(
                Status)
                ? "Unknown"
                : Status;


        public string ImpactDisplay =>
            string.IsNullOrWhiteSpace(
                BusinessImpact)
                ? "Impact: Not specified"
                : $"Impact: {BusinessImpact}";


        public string NotesDisplay =>
            string.IsNullOrWhiteSpace(
                Notes)
                ? "No additional notes"
                : Notes;


        public string DateDisplay =>
            RecordDate.ToString(
                "dd MMM yyyy");


        public string SubmittedDisplay =>
            SubmittedAt == default
                ? string.Empty
                : $"Submitted {SubmittedAt:dd MMM yyyy HH:mm}";
    }


    // =============================================================
    // EMPLOYEE OBJECT RETURNED INSIDE EACH DAILY RECORD
    // =============================================================

    public class EmployeeRecordEmployeeDto
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