using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InsightFlow
{
    public partial class RegionsPage : ContentPage
    {
        private const string ApiBaseUrl = "https://localhost:7212/";

        private readonly HttpClient _httpClient;

        private readonly List<DepartmentDailyRecordDto> _records = new();

        // =========================================================
        // JSON OPTIONS
        // =========================================================

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        // =========================================================
        // CONFIGURED DEPARTMENTS
        // =========================================================

        private static readonly List<DepartmentDefinition> Departments =
            new()
            {
                new DepartmentDefinition
                {
                    Name = "Sales",
                    Code = "IF-SAL"
                },

                new DepartmentDefinition
                {
                    Name = "Finance",
                    Code = "IF-FIN"
                },

                new DepartmentDefinition
                {
                    Name = "Human Resources",
                    Code = "IF-HR"
                },

                new DepartmentDefinition
                {
                    Name = "Information Technology",
                    Code = "IF-IT"
                },

                new DepartmentDefinition
                {
                    Name = "Marketing",
                    Code = "IF-MKT"
                },

                new DepartmentDefinition
                {
                    Name = "Operations",
                    Code = "IF-OPS"
                },

                new DepartmentDefinition
                {
                    Name = "Customer Service",
                    Code = "IF-CS"
                },

                new DepartmentDefinition
                {
                    Name = "Procurement",
                    Code = "IF-PRC"
                },

                new DepartmentDefinition
                {
                    Name = "Management",
                    Code = "IF-MGT"
                }
            };

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public RegionsPage()
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

            await LoadDepartmentDataAsync();
        }

        // =========================================================
        // AUTHENTICATION
        // =========================================================

        private async Task<bool> ConfigureAuthenticationAsync()
        {
            string? token =
                await SecureStorage.Default.GetAsync("auth_token");

            if (string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;

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

        // =========================================================
        // LOAD DATA
        // =========================================================

        private async Task LoadDepartmentDataAsync()
        {
            try
            {
                SetLoading(true);

                HideError();

                _records.Clear();

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
                        "Administrator or Manager access is required to view department analytics.");

                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await ReadErrorMessageAsync(
                            response);

                    ShowError(
                        string.IsNullOrWhiteSpace(error)
                            ? "Unable to load department records."
                            : error);

                    return;
                }

                string json =
                    await response.Content
                        .ReadAsStringAsync();

                List<DepartmentDailyRecordDto> records =
                    ParseDailyRecords(json);

                _records.AddRange(records);

                CalculateCompanyMetrics();

                BuildDepartmentComparison();

                ApplySearchFilter();

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
                    $"The API returned data in an unexpected format: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError(
                    $"Unable to load department analytics: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        // =========================================================
        // PARSE DAILY RECORDS
        // =========================================================

        private static List<DepartmentDailyRecordDto>
            ParseDailyRecords(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<DepartmentDailyRecordDto>();
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
                return DeserializeRecordArray(root);
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
                    return DeserializeRecordArray(values);
                }

                // =================================================
                // FORMAT 3:
                // {
                //     "success": true,
                //     "count": 1,
                //     "records": [...]
                // }
                // =================================================

                if (TryGetArrayProperty(
                        root,
                        "records",
                        out JsonElement records))
                {
                    return DeserializeRecordArray(records);
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
                    return DeserializeRecordArray(data);
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
                    return DeserializeRecordArray(items);
                }

                // =================================================
                // FORMAT 6:
                // {
                //     "data":
                //     {
                //         "$values": [...]
                //     }
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
                        out JsonElement nestedValues))
                {
                    return DeserializeRecordArray(
                        nestedValues);
                }

                // =================================================
                // FORMAT 7:
                // {
                //     "records":
                //     {
                //         "$values": [...]
                //     }
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
                // FORMAT 8:
                // {
                //     "items":
                //     {
                //         "$values": [...]
                //     }
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
                "DailyRecords response did not contain a supported record collection.");
        }

        // =========================================================
        // DESERIALIZE ARRAY
        // =========================================================

        private static List<DepartmentDailyRecordDto>
            DeserializeRecordArray(
                JsonElement array)
        {
            if (array.ValueKind !=
                JsonValueKind.Array)
            {
                return new List<DepartmentDailyRecordDto>();
            }

            List<DepartmentDailyRecordDto>? result =
                JsonSerializer.Deserialize<
                    List<DepartmentDailyRecordDto>>(
                        array.GetRawText(),
                        JsonOptions);

            return result ??
                   new List<DepartmentDailyRecordDto>();
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
        // CASE-INSENSITIVE JSON PROPERTY
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
        // COMPANY METRICS
        // =========================================================

        private void CalculateCompanyMetrics()
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

            decimal businessValue =
                _records.Sum(
                    record =>
                        record.BusinessValue);

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

            int activeDepartments =
                Departments.Count(
                    department =>
                        _records.Any(
                            record =>
                                IsDepartmentRecord(
                                    record,
                                    department)));

            double completionRate =
                totalActivities == 0
                    ? 0
                    : (double)completed /
                      totalActivities * 100;

            DepartmentsLabel.Text =
                Departments.Count.ToString("N0");

            TotalActivitiesLabel.Text =
                totalActivities.ToString("N0");

            ActiveDepartmentsLabel.Text =
                activeDepartments.ToString("N0");

            BusinessValueLabel.Text =
                $"R {businessValue:N2}";

            CompletedLabel.Text =
                completed.ToString("N0");

            CompletionRateLabel.Text =
                $"{completionRate:N1}%";

            ContributorsLabel.Text =
                contributors.ToString("N0");

            StatusCompletedLabel.Text =
                completed.ToString("N0");

            StatusInProgressLabel.Text =
                inProgress.ToString("N0");

            StatusPendingLabel.Text =
                pending.ToString("N0");

            StatusOnHoldLabel.Text =
                onHold.ToString("N0");
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
                        record.Status?.Trim(),
                        status,
                        StringComparison.OrdinalIgnoreCase));
        }

        // =========================================================
        // BUILD DEPARTMENT COMPARISON
        // =========================================================

        private void BuildDepartmentComparison()
        {
            List<DepartmentAnalyticsItem>
                departmentItems = new();

            foreach (DepartmentDefinition department
                     in Departments)
            {
                List<DepartmentDailyRecordDto>
                    departmentRecords =
                        _records
                            .Where(
                                record =>
                                    IsDepartmentRecord(
                                        record,
                                        department))
                            .ToList();

                int activityCount =
                    departmentRecords.Count;

                int completed =
                    departmentRecords.Count(
                        record =>
                            string.Equals(
                                record.Status?.Trim(),
                                "Completed",
                                StringComparison.OrdinalIgnoreCase));

                decimal businessValue =
                    departmentRecords.Sum(
                        record =>
                            record.BusinessValue);

                long quantity =
                    departmentRecords.Sum(
                        record =>
                            (long)record.Quantity);

                long secondaryMetric =
                    departmentRecords.Sum(
                        record =>
                            (long)record.SecondaryMetric);

                int contributors =
                    departmentRecords
                        .Where(
                            record =>
                                record.EmployeeAccountId > 0)
                        .Select(
                            record =>
                                record.EmployeeAccountId)
                        .Distinct()
                        .Count();

                double completionRate =
                    activityCount == 0
                        ? 0
                        : (double)completed /
                          activityCount * 100;

                departmentItems.Add(
                    new DepartmentAnalyticsItem
                    {
                        DepartmentName =
                            department.Name,

                        DepartmentCode =
                            department.Code,

                        ActivityCount =
                            activityCount,

                        Completed =
                            completed,

                        CompletionRate =
                            completionRate,

                        BusinessValue =
                            businessValue,

                        Quantity =
                            quantity,

                        SecondaryMetric =
                            secondaryMetric,

                        Contributors =
                            contributors
                    });
            }

            DepartmentCollectionView.ItemsSource =
                departmentItems
                    .OrderByDescending(
                        item =>
                            item.ActivityCount)
                    .ThenBy(
                        item =>
                            item.DepartmentName)
                    .ToList();
        }

        // =========================================================
        // DEPARTMENT MATCHING
        // =========================================================

        private static bool IsDepartmentRecord(
            DepartmentDailyRecordDto record,
            DepartmentDefinition department)
        {
            // Match the full department name.
            if (string.Equals(
                    record.Department.Trim(),
                    department.Name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Match the complete InsightFlow department code.
            if (string.Equals(
                    record.DepartmentCode.Trim(),
                    department.Code,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Also support short department codes if required.
            string shortCode =
                department.Code.StartsWith(
                    "IF-",
                    StringComparison.OrdinalIgnoreCase)
                    ? department.Code.Substring(3)
                    : department.Code;

            return string.Equals(
                record.DepartmentCode.Trim(),
                shortCode,
                StringComparison.OrdinalIgnoreCase);
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

            IEnumerable<DepartmentDailyRecordDto>
                result =
                    _records;

            if (!string.IsNullOrWhiteSpace(
                    search))
            {
                result =
                    result.Where(
                        record =>

                            ContainsText(
                                record.Department,
                                search)

                            ||

                            ContainsText(
                                record.DepartmentCode,
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

            List<DepartmentDailyRecordDto>
                displayRecords =
                    result
                        .OrderByDescending(
                            record =>
                                record.SubmittedAt)
                        .ThenByDescending(
                            record =>
                                record.RecordDate)
                        .ToList();

            RecordsCollectionView.ItemsSource =
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
            DepartmentsLabel.Text =
                Departments.Count.ToString("N0");

            TotalActivitiesLabel.Text =
                "0";

            ActiveDepartmentsLabel.Text =
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

            RecordCountLabel.Text =
                "0 records";

            DepartmentCollectionView.ItemsSource =
                BuildEmptyDepartmentList();

            RecordsCollectionView.ItemsSource =
                null;

            LastUpdatedLabel.Text =
                "Waiting for department data";
        }

        // =========================================================
        // EMPTY DEPARTMENT LIST
        // =========================================================

        private static List<DepartmentAnalyticsItem>
            BuildEmptyDepartmentList()
        {
            return Departments
                .Select(
                    department =>
                        new DepartmentAnalyticsItem
                        {
                            DepartmentName =
                                department.Name,

                            DepartmentCode =
                                department.Code,

                            ActivityCount = 0,

                            Completed = 0,

                            CompletionRate = 0,

                            BusinessValue = 0,

                            Quantity = 0,

                            SecondaryMetric = 0,

                            Contributors = 0
                        })
                .ToList();
        }

        // =========================================================
        // REFRESH
        // =========================================================

        private async void OnRefreshClicked(
            object sender,
            EventArgs e)
        {
            await LoadDepartmentDataAsync();
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
    // DEPARTMENT DEFINITION
    // =============================================================

    public class DepartmentDefinition
    {
        public string Name { get; set; } =
            string.Empty;

        public string Code { get; set; } =
            string.Empty;
    }

    // =============================================================
    // DAILY RECORD DTO
    // =============================================================

    public class DepartmentDailyRecordDto
    {
        public long RecordId { get; set; }

        // =========================================================
        // NESTED EMPLOYEE RETURNED BY THE API
        // =========================================================

        public DepartmentEmployeeDto? Employee
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
        // RESOLVED EMPLOYEE VALUES
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
    }

    // =============================================================
    // NESTED EMPLOYEE DTO
    // =============================================================

    public class DepartmentEmployeeDto
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
    // DEPARTMENT ANALYTICS ITEM
    // =============================================================

    public class DepartmentAnalyticsItem
    {
        public string DepartmentName { get; set; } =
            string.Empty;

        public string DepartmentCode { get; set; } =
            string.Empty;

        public int ActivityCount { get; set; }

        public int Completed { get; set; }

        public double CompletionRate { get; set; }

        public decimal BusinessValue { get; set; }

        public long Quantity { get; set; }

        public long SecondaryMetric { get; set; }

        public int Contributors { get; set; }

        // =========================================================
        // DISPLAY PROPERTIES
        // =========================================================

        public string BusinessValueDisplay =>
            $"R {BusinessValue:N2}";

        public string CompletionRateDisplay =>
            $"{CompletionRate:N1}%";

        public string ActivityDisplay =>
            ActivityCount == 1
                ? "1 activity"
                : $"{ActivityCount:N0} activities";

        public string ContributorsDisplay =>
            Contributors == 1
                ? "1 contributor"
                : $"{Contributors:N0} contributors";

        public string QuantityDisplay =>
            Quantity.ToString("N0");

        public string SecondaryMetricDisplay =>
            SecondaryMetric.ToString("N0");
    }
}