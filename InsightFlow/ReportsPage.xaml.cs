using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using InsightFlow.Services;

namespace InsightFlow
{
    public partial class ReportsPage : ContentPage
    {
        // =========================================================
        // API
        // =========================================================

        private const string ApiBaseUrl =
            "https://localhost:7212/";

        private readonly HttpClient _httpClient;


        // =========================================================
        // DATA
        // =========================================================

        private readonly List<ReportDailyRecordDto>
            _records = new();

        private readonly List<ReportEmployeeOption>
            _employees = new();

        private readonly List<ReportDailyRecordDto>
            _currentReportRecords = new();


        // =========================================================
        // PDF SERVICE
        // =========================================================

        private readonly ReportPdfService
            _reportPdfService = new();


        // =========================================================
        // JSON
        // =========================================================

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ReportsPage()
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

            SetupDefaultFilters();

            ResetReport();
        }


        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadReportDataAsync();
        }


        // =========================================================
        // DEFAULT FILTERS
        // =========================================================

        private void SetupDefaultFilters()
        {
            ReportScopePicker.SelectedIndex =
                0;

            DepartmentPicker.SelectedIndex =
                0;

            StatusPicker.SelectedIndex =
                0;

            EndDatePicker.Date =
                DateTime.Today;

            StartDatePicker.Date =
                DateTime.Today.AddMonths(-1);

            DepartmentPicker.IsEnabled =
                false;

            EmployeePicker.IsEnabled =
                false;
        }


        // =========================================================
        // AUTHENTICATION
        // =========================================================

        private async Task<bool> ConfigureAuthenticationAsync()
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
        // LOAD REPORT DATA
        // =========================================================

        private async Task LoadReportDataAsync()
        {
            try
            {
                SetLoading(true);

                HideError();

                _records.Clear();

                _employees.Clear();

                _currentReportRecords.Clear();

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
                        "Administrator or Manager access is required to view reports.");

                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await ReadErrorMessageAsync(
                            response);

                    ShowError(
                        string.IsNullOrWhiteSpace(error)
                            ? "Unable to load report records."
                            : error);

                    return;
                }

                string json =
                    await response.Content
                        .ReadAsStringAsync();

                List<ReportDailyRecordDto> records =
                    ParseReportRecords(json);

                _records.AddRange(
                    records);

                BuildEmployeeOptions();

                GenerateReport();

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
                    $"The API returned report data in an unexpected format: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError(
                    $"Unable to load reports: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }


        // =========================================================
        // PARSE REPORT RECORDS
        // =========================================================

        private static List<ReportDailyRecordDto>
            ParseReportRecords(
                string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<ReportDailyRecordDto>();
            }

            using JsonDocument document =
                JsonDocument.Parse(json);

            JsonElement root =
                document.RootElement;


            // API returned direct array
            if (root.ValueKind ==
                JsonValueKind.Array)
            {
                return DeserializeRecordArray(
                    root);
            }


            // API returned object wrapper
            if (root.ValueKind ==
                JsonValueKind.Object)
            {
                // $values
                if (TryGetArrayProperty(
                        root,
                        "$values",
                        out JsonElement values))
                {
                    return DeserializeRecordArray(
                        values);
                }


                // records
                if (TryGetArrayProperty(
                        root,
                        "records",
                        out JsonElement records))
                {
                    return DeserializeRecordArray(
                        records);
                }


                // data
                if (TryGetArrayProperty(
                        root,
                        "data",
                        out JsonElement data))
                {
                    return DeserializeRecordArray(
                        data);
                }


                // items
                if (TryGetArrayProperty(
                        root,
                        "items",
                        out JsonElement items))
                {
                    return DeserializeRecordArray(
                        items);
                }


                // records -> $values
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


                // data -> $values
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


                // items -> $values
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
                "DailyRecords response did not contain a supported report-record collection.");
        }


        // =========================================================
        // DESERIALIZE RECORD ARRAY
        // =========================================================

        private static List<ReportDailyRecordDto>
            DeserializeRecordArray(
                JsonElement array)
        {
            if (array.ValueKind !=
                JsonValueKind.Array)
            {
                return new List<ReportDailyRecordDto>();
            }

            List<ReportDailyRecordDto>? records =
                JsonSerializer.Deserialize<
                    List<ReportDailyRecordDto>>(
                        array.GetRawText(),
                        JsonOptions);

            return records ??
                   new List<ReportDailyRecordDto>();
        }


        // =========================================================
        // TRY GET ARRAY PROPERTY
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

            value =
                default;

            return false;
        }


        // =========================================================
        // CASE INSENSITIVE JSON PROPERTY
        // =========================================================

        private static bool TryGetPropertyIgnoreCase(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            if (element.ValueKind !=
                JsonValueKind.Object)
            {
                value =
                    default;

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

            value =
                default;

            return false;
        }


        // =========================================================
        // BUILD EMPLOYEE OPTIONS
        // =========================================================

        private void BuildEmployeeOptions()
        {
            _employees.Clear();


            // All Employees option
            _employees.Add(
                new ReportEmployeeOption
                {
                    EmployeeAccountId =
                        0,

                    EmployeeId =
                        string.Empty,

                    EmployeeName =
                        "All Employees",

                    Department =
                        string.Empty
                });


            List<ReportEmployeeOption> employees =
                _records

                    .Where(
                        record =>
                            record.EmployeeAccountId > 0)

                    .GroupBy(
                        record =>
                            record.EmployeeAccountId)

                    .Select(
                        group =>
                        {
                            ReportDailyRecordDto record =
                                group.First();

                            return new ReportEmployeeOption
                            {
                                EmployeeAccountId =
                                    record.EmployeeAccountId,

                                EmployeeId =
                                    record.EmployeeId,

                                EmployeeName =
                                    string.IsNullOrWhiteSpace(
                                        record.EmployeeName)

                                        ? record.EmployeeId

                                        : record.EmployeeName,

                                Department =
                                    record.Department
                            };
                        })

                    .OrderBy(
                        employee =>
                            employee.EmployeeName)

                    .ToList();


            _employees.AddRange(
                employees);


            RefreshEmployeePicker();
        }


        // =========================================================
        // REFRESH EMPLOYEE PICKER
        // =========================================================

        private void RefreshEmployeePicker()
        {
            string selectedDepartment =
                DepartmentPicker
                    .SelectedItem?
                    .ToString()
                    ?.Trim()
                ?? "All Departments";


            List<ReportEmployeeOption>
                employeeOptions;


            if (string.Equals(
                    selectedDepartment,
                    "All Departments",
                    StringComparison.OrdinalIgnoreCase))
            {
                employeeOptions =
                    _employees.ToList();
            }
            else
            {
                employeeOptions =
                    _employees

                        .Where(
                            employee =>

                                employee.EmployeeAccountId == 0

                                ||

                                string.Equals(
                                    employee.Department?.Trim(),
                                    selectedDepartment,
                                    StringComparison.OrdinalIgnoreCase))

                        .ToList();
            }


            EmployeePicker.ItemsSource =
                employeeOptions;


            EmployeePicker.ItemDisplayBinding =
                new Binding(
                    nameof(
                        ReportEmployeeOption.DisplayName));


            EmployeePicker.SelectedIndex =
                employeeOptions.Count > 0
                    ? 0
                    : -1;
        }


        // =========================================================
        // REPORT SCOPE CHANGED
        // =========================================================

        private void OnReportScopeChanged(
            object sender,
            EventArgs e)
        {
            string scope =
                ReportScopePicker
                    .SelectedItem?
                    .ToString()
                    ?.Trim()
                ?? "Company";


            // COMPANY
            if (string.Equals(
                    scope,
                    "Company",
                    StringComparison.OrdinalIgnoreCase))
            {
                DepartmentPicker.IsEnabled =
                    false;

                EmployeePicker.IsEnabled =
                    false;

                DepartmentPicker.SelectedIndex =
                    0;

                if (EmployeePicker.ItemsSource != null)
                {
                    EmployeePicker.SelectedIndex =
                        0;
                }

                ReportFilterSummaryLabel.Text =
                    "Company";

                return;
            }


            // DEPARTMENT
            if (string.Equals(
                    scope,
                    "Department",
                    StringComparison.OrdinalIgnoreCase))
            {
                DepartmentPicker.IsEnabled =
                    true;

                EmployeePicker.IsEnabled =
                    false;

                if (EmployeePicker.ItemsSource != null)
                {
                    EmployeePicker.SelectedIndex =
                        0;
                }

                ReportFilterSummaryLabel.Text =
                    "Department";

                return;
            }


            // EMPLOYEE
            DepartmentPicker.IsEnabled =
                true;

            EmployeePicker.IsEnabled =
                true;

            ReportFilterSummaryLabel.Text =
                "Employee";
        }


        // =========================================================
        // DEPARTMENT CHANGED
        // =========================================================

        private void OnDepartmentChanged(
            object sender,
            EventArgs e)
        {
            if (_employees.Count > 0)
            {
                RefreshEmployeePicker();
            }
        }


        // =========================================================
        // GENERATE REPORT BUTTON
        // =========================================================

        private async void OnGenerateReportClicked(
            object sender,
            EventArgs e)
        {
            try
            {
                GenerateReportButton.IsEnabled =
                    false;

                GenerateReportButton.Text =
                    "GENERATING...";

                HideError();

                GenerateReport();

                if (!ErrorBorder.IsVisible)
                {
                    LastUpdatedLabel.Text =
                        $"Report generated: {DateTime.Now:dd MMM yyyy, HH:mm:ss}";
                }

                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                ShowError(
                    $"Unable to generate report: {ex.Message}");
            }
            finally
            {
                GenerateReportButton.Text =
                    "GENERATE REPORT";

                GenerateReportButton.IsEnabled =
                    true;
            }
        }


        // =========================================================
        // GENERATE PDF BUTTON
        // =========================================================

        private async void OnGeneratePdfClicked(
            object sender,
            EventArgs e)
        {
            try
            {
                HideError();

                GeneratePdfButton.IsEnabled =
                    false;

                GeneratePdfButton.Text =
                    "GENERATING PDF...";


                // ---------------------------------------------
                // APPLY CURRENT FILTERS FIRST
                // ---------------------------------------------

                GenerateReport();


                // GenerateReport may have produced a validation error
                if (ErrorBorder.IsVisible)
                {
                    return;
                }


                // ---------------------------------------------
                // MAKE SURE WE HAVE DATA
                // ---------------------------------------------

                if (_currentReportRecords.Count == 0)
                {
                    await DisplayAlert(
                        "No Report Data",
                        "There are no records matching the selected report filters.",
                        "OK");

                    return;
                }


                // ---------------------------------------------
                // PDF INFORMATION
                // ---------------------------------------------

                string reportTitle =
                    ReportTitleLabel.Text
                    ?? "InsightFlow Performance Report";


                string reportPeriod =
                    ReportPeriodLabel.Text
                    ?? string.Empty;


                string filterSummary =
                    ReportFilterSummaryLabel.Text
                    ?? string.Empty;


                // ---------------------------------------------
                // GENERATE QUESTPDF DOCUMENT
                // ---------------------------------------------

                byte[] pdfBytes =
                    _reportPdfService.GenerateReport(
                        reportTitle,
                        reportPeriod,
                        filterSummary,
                        _currentReportRecords.ToList());


                if (pdfBytes == null ||
                    pdfBytes.Length == 0)
                {
                    ShowError(
                        "The PDF could not be generated.");

                    return;
                }


                // ---------------------------------------------
                // CREATE FILE NAME
                // ---------------------------------------------

                string safeTitle =
                    CreateSafeFileName(
                        reportTitle);


                string fileName =
                    $"InsightFlow_{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";


                string filePath =
                    Path.Combine(
                        FileSystem.CacheDirectory,
                        fileName);


                // ---------------------------------------------
                // SAVE PDF
                // ---------------------------------------------

                await File.WriteAllBytesAsync(
                    filePath,
                    pdfBytes);


                LastUpdatedLabel.Text =
                    $"PDF generated: {DateTime.Now:dd MMM yyyy, HH:mm:ss}";


                // ---------------------------------------------
                // OPEN PDF
                // ---------------------------------------------

                await Launcher.Default.OpenAsync(
                    new OpenFileRequest(
                        "Open InsightFlow Report",
                        new ReadOnlyFile(
                            filePath)));
            }
            catch (Exception ex)
            {
                ShowError(
                    $"Unable to generate PDF report: {ex.Message}");
            }
            finally
            {
                GeneratePdfButton.Text =
                    "GENERATE PDF";

                GeneratePdfButton.IsEnabled =
                    true;
            }
        }


        // =========================================================
        // SAFE PDF FILE NAME
        // =========================================================

        private static string CreateSafeFileName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Performance_Report";
            }

            char[] invalidCharacters =
                Path.GetInvalidFileNameChars();


            string cleaned =
                new string(
                    value

                        .Select(
                            character =>
                                invalidCharacters.Contains(
                                    character)

                                    ? '_'

                                    : character)

                        .ToArray());


            cleaned =
                cleaned
                    .Replace(
                        " ",
                        "_")
                    .Trim('_');


            return string.IsNullOrWhiteSpace(cleaned)
                ? "Performance_Report"
                : cleaned;
        }


        // =========================================================
        // GENERATE REPORT
        // =========================================================

        private void GenerateReport()
        {
            HideError();


            // ---------------------------------------------
            // DATES
            // ---------------------------------------------

            DateTime startDate =
                StartDatePicker.Date.Date;


            DateTime endDate =
                EndDatePicker.Date.Date;


            if (startDate > endDate)
            {
                _currentReportRecords.Clear();

                ShowError(
                    "Start Date cannot be later than End Date.");

                return;
            }


            // ---------------------------------------------
            // FILTER VALUES
            // ---------------------------------------------

            string scope =
                ReportScopePicker
                    .SelectedItem?
                    .ToString()
                    ?.Trim()
                ?? "Company";


            string department =
                DepartmentPicker
                    .SelectedItem?
                    .ToString()
                    ?.Trim()
                ?? "All Departments";


            string status =
                StatusPicker
                    .SelectedItem?
                    .ToString()
                    ?.Trim()
                ?? "All Statuses";


            ReportEmployeeOption? employee =
                EmployeePicker.SelectedItem
                    as ReportEmployeeOption;


            // ---------------------------------------------
            // DATE FILTER
            // ---------------------------------------------

            IEnumerable<ReportDailyRecordDto> result =
                _records.Where(
                    record =>
                        record.RecordDate.Date >=
                        startDate

                        &&

                        record.RecordDate.Date <=
                        endDate);


            // =====================================================
            // DEPARTMENT REPORT
            // =====================================================

            if (string.Equals(
                    scope,
                    "Department",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(
                        department,
                        "All Departments",
                        StringComparison.OrdinalIgnoreCase))
                {
                    _currentReportRecords.Clear();

                    ShowError(
                        "Select a department before generating a Department report.");

                    return;
                }


                result =
                    result.Where(
                        record =>
                            string.Equals(
                                record.Department?.Trim(),
                                department,
                                StringComparison.OrdinalIgnoreCase));
            }


            // =====================================================
            // EMPLOYEE REPORT
            // =====================================================

            if (string.Equals(
                    scope,
                    "Employee",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (employee == null ||
                    employee.EmployeeAccountId <= 0)
                {
                    _currentReportRecords.Clear();

                    ShowError(
                        "Select an employee before generating an Employee report.");

                    return;
                }


                result =
                    result.Where(
                        record =>
                            record.EmployeeAccountId ==
                            employee.EmployeeAccountId);
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


            // =====================================================
            // BUILD RESULT
            // =====================================================

            List<ReportDailyRecordDto> reportRecords =
                result

                    .OrderByDescending(
                        record =>
                            record.RecordDate)

                    .ThenByDescending(
                        record =>
                            record.SubmittedAt)

                    .ToList();


            // =====================================================
            // STORE FILTERED RECORDS FOR PDF
            // =====================================================

            _currentReportRecords.Clear();

            _currentReportRecords.AddRange(
                reportRecords);


            // =====================================================
            // UPDATE REPORT TITLE
            // =====================================================

            UpdateReportTitle(
                scope,
                department,
                employee,
                startDate,
                endDate,
                status);


            // =====================================================
            // CALCULATE KPIs
            // =====================================================

            CalculateReportMetrics(
                reportRecords);


            // =====================================================
            // BUILD BREAKDOWN
            // =====================================================

            BuildBreakdown(
                reportRecords,
                scope);


            // =====================================================
            // DISPLAY RECORDS
            // =====================================================

            ReportRecordsCollectionView.ItemsSource =
                null;


            ReportRecordsCollectionView.ItemsSource =
                reportRecords;


            RecordCountLabel.Text =
                reportRecords.Count == 1
                    ? "1 record"
                    : $"{reportRecords.Count} records";
        }


        // =========================================================
        // UPDATE REPORT TITLE
        // =========================================================

        private void UpdateReportTitle(
            string scope,
            string department,
            ReportEmployeeOption? employee,
            DateTime startDate,
            DateTime endDate,
            string status)
        {
            // ---------------------------------------------
            // REPORT TITLE
            // ---------------------------------------------

            if (string.Equals(
                    scope,
                    "Department",
                    StringComparison.OrdinalIgnoreCase))
            {
                ReportTitleLabel.Text =
                    $"{department} Performance Report";
            }
            else if (string.Equals(
                         scope,
                         "Employee",
                         StringComparison.OrdinalIgnoreCase))
            {
                ReportTitleLabel.Text =
                    employee == null
                        ? "Employee Performance Report"
                        : $"{employee.EmployeeName} Performance Report";
            }
            else
            {
                ReportTitleLabel.Text =
                    "Company Performance Report";
            }


            // ---------------------------------------------
            // PERIOD
            // ---------------------------------------------

            ReportPeriodLabel.Text =
                $"{startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}";


            // ---------------------------------------------
            // FILTER SUMMARY
            // ---------------------------------------------

            List<string> summary =
                new()
                {
                    scope
                };


            if (string.Equals(
                    scope,
                    "Department",
                    StringComparison.OrdinalIgnoreCase))
            {
                summary.Add(
                    department);
            }


            if (string.Equals(
                    scope,
                    "Employee",
                    StringComparison.OrdinalIgnoreCase)

                &&

                employee != null)
            {
                summary.Add(
                    employee.DisplayName);
            }


            if (!string.Equals(
                    status,
                    "All Statuses",
                    StringComparison.OrdinalIgnoreCase))
            {
                summary.Add(
                    status);
            }


            ReportFilterSummaryLabel.Text =
                string.Join(
                    " • ",
                    summary);
        }


        // =========================================================
        // CALCULATE REPORT METRICS
        // =========================================================

        private void CalculateReportMetrics(
            List<ReportDailyRecordDto> records)
        {
            int total =
                records.Count;


            int completed =
                records.Count(
                    record =>
                        string.Equals(
                            record.Status?.Trim(),
                            "Completed",
                            StringComparison.OrdinalIgnoreCase));


            int inProgress =
                records.Count(
                    record =>
                        string.Equals(
                            record.Status?.Trim(),
                            "In Progress",
                            StringComparison.OrdinalIgnoreCase));


            int pending =
                records.Count(
                    record =>
                        string.Equals(
                            record.Status?.Trim(),
                            "Pending",
                            StringComparison.OrdinalIgnoreCase));


            int onHold =
                records.Count(
                    record =>
                        string.Equals(
                            record.Status?.Trim(),
                            "On Hold",
                            StringComparison.OrdinalIgnoreCase));


            int contributors =
                records

                    .Where(
                        record =>
                            record.EmployeeAccountId > 0)

                    .Select(
                        record =>
                            record.EmployeeAccountId)

                    .Distinct()

                    .Count();


            long quantity =
                records.Sum(
                    record =>
                        (long)record.Quantity);


            decimal businessValue =
                records.Sum(
                    record =>
                        record.BusinessValue);


            double completionRate =
                total == 0
                    ? 0
                    : (double)completed /
                      total * 100;


            TotalActivitiesLabel.Text =
                total.ToString("N0");


            CompletedLabel.Text =
                completed.ToString("N0");


            CompletionRateLabel.Text =
                $"{completionRate:N1}%";


            BusinessValueLabel.Text =
                $"R {businessValue:N2}";


            ContributorsLabel.Text =
                contributors.ToString("N0");


            TotalQuantityLabel.Text =
                quantity.ToString("N0");


            InProgressLabel.Text =
                inProgress.ToString("N0");


            PendingHoldLabel.Text =
                (pending + onHold)
                    .ToString("N0");
        }


        // =========================================================
        // BUILD BREAKDOWN
        // =========================================================

        private void BuildBreakdown(
            List<ReportDailyRecordDto> records,
            string scope)
        {
            List<ReportBreakdownItem>
                breakdown;


            // =====================================================
            // COMPANY -> DEPARTMENTS
            // =====================================================

            if (string.Equals(
                    scope,
                    "Company",
                    StringComparison.OrdinalIgnoreCase))
            {
                BreakdownTitleLabel.Text =
                    "Department Breakdown";


                breakdown =
                    records

                        .GroupBy(
                            record =>
                                string.IsNullOrWhiteSpace(
                                    record.Department)

                                    ? "Unknown Department"

                                    : record.Department.Trim(),
                            StringComparer.OrdinalIgnoreCase)

                        .Select(
                            group =>
                                CreateBreakdownItem(
                                    group.Key,
                                    group.ToList()))

                        .OrderByDescending(
                            item =>
                                item.Activities)

                        .ToList();
            }


            // =====================================================
            // DEPARTMENT -> EMPLOYEES
            // =====================================================

            else if (string.Equals(
                         scope,
                         "Department",
                         StringComparison.OrdinalIgnoreCase))
            {
                BreakdownTitleLabel.Text =
                    "Employee Breakdown";


                breakdown =
                    records

                        .GroupBy(
                            record =>
                                string.IsNullOrWhiteSpace(
                                    record.EmployeeName)

                                    ? record.EmployeeId

                                    : record.EmployeeName,
                            StringComparer.OrdinalIgnoreCase)

                        .Select(
                            group =>
                                CreateBreakdownItem(
                                    group.Key,
                                    group.ToList()))

                        .OrderByDescending(
                            item =>
                                item.Activities)

                        .ToList();
            }


            // =====================================================
            // EMPLOYEE -> ACTIVITY TYPES
            // =====================================================

            else
            {
                BreakdownTitleLabel.Text =
                    "Activity Breakdown";


                breakdown =
                    records

                        .GroupBy(
                            record =>
                                string.IsNullOrWhiteSpace(
                                    record.ActivityType)

                                    ? "Unknown Activity"

                                    : record.ActivityType.Trim(),
                            StringComparer.OrdinalIgnoreCase)

                        .Select(
                            group =>
                                CreateBreakdownItem(
                                    group.Key,
                                    group.ToList()))

                        .OrderByDescending(
                            item =>
                                item.Activities)

                        .ToList();
            }


            BreakdownCollectionView.ItemsSource =
                null;


            BreakdownCollectionView.ItemsSource =
                breakdown;
        }


        // =========================================================
        // CREATE BREAKDOWN ITEM
        // =========================================================

        private static ReportBreakdownItem
            CreateBreakdownItem(
                string name,
                List<ReportDailyRecordDto> records)
        {
            int activities =
                records.Count;


            int completed =
                records.Count(
                    record =>
                        string.Equals(
                            record.Status?.Trim(),
                            "Completed",
                            StringComparison.OrdinalIgnoreCase));


            double completionRate =
                activities == 0
                    ? 0
                    : (double)completed /
                      activities * 100;


            decimal value =
                records.Sum(
                    record =>
                        record.BusinessValue);


            return new ReportBreakdownItem
            {
                Name =
                    name,

                Activities =
                    activities,

                Completed =
                    completed,

                CompletionRate =
                    completionRate,

                BusinessValue =
                    value
            };
        }


        // =========================================================
        // RESET FILTERS
        // =========================================================

        private async void OnResetFiltersClicked(
            object sender,
            EventArgs e)
        {
            try
            {
                ResetReportButton.IsEnabled =
                    false;

                ResetReportButton.Text =
                    "RESETTING...";

                HideError();


                // ---------------------------------------------
                // SCOPE
                // ---------------------------------------------

                ReportScopePicker.SelectedIndex =
                    0;


                // ---------------------------------------------
                // DEPARTMENT
                // ---------------------------------------------

                DepartmentPicker.SelectedIndex =
                    0;


                // ---------------------------------------------
                // STATUS
                // ---------------------------------------------

                StatusPicker.SelectedIndex =
                    0;


                // ---------------------------------------------
                // DATE RANGE
                // ---------------------------------------------

                EndDatePicker.Date =
                    DateTime.Today;


                StartDatePicker.Date =
                    DateTime.Today.AddMonths(-1);


                // ---------------------------------------------
                // EMPLOYEES
                // ---------------------------------------------

                RefreshEmployeePicker();


                if (EmployeePicker.ItemsSource != null)
                {
                    EmployeePicker.SelectedIndex =
                        0;
                }


                // ---------------------------------------------
                // COMPANY DEFAULT
                // ---------------------------------------------

                DepartmentPicker.IsEnabled =
                    false;


                EmployeePicker.IsEnabled =
                    false;


                // ---------------------------------------------
                // GENERATE DEFAULT REPORT
                // ---------------------------------------------

                GenerateReport();


                LastUpdatedLabel.Text =
                    $"Filters reset: {DateTime.Now:dd MMM yyyy, HH:mm:ss}";


                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                ShowError(
                    $"Unable to reset report filters: {ex.Message}");
            }
            finally
            {
                ResetReportButton.Text =
                    "RESET";


                ResetReportButton.IsEnabled =
                    true;
            }
        }


        // =========================================================
        // RESET REPORT DISPLAY
        // =========================================================

        private void ResetReport()
        {
            _currentReportRecords.Clear();


            TotalActivitiesLabel.Text =
                "0";


            CompletedLabel.Text =
                "0";


            CompletionRateLabel.Text =
                "0%";


            BusinessValueLabel.Text =
                "R 0.00";


            ContributorsLabel.Text =
                "0";


            TotalQuantityLabel.Text =
                "0";


            InProgressLabel.Text =
                "0";


            PendingHoldLabel.Text =
                "0";


            RecordCountLabel.Text =
                "0 records";


            BreakdownCollectionView.ItemsSource =
                null;


            ReportRecordsCollectionView.ItemsSource =
                null;


            ReportTitleLabel.Text =
                "Company Performance Report";


            ReportPeriodLabel.Text =
                "No report generated";


            ReportFilterSummaryLabel.Text =
                "Company";


            LastUpdatedLabel.Text =
                "Waiting for report data";
        }


        // =========================================================
        // REFRESH
        // =========================================================

        private async void OnRefreshClicked(
            object sender,
            EventArgs e)
        {
            try
            {
                await LoadReportDataAsync();
            }
            catch (Exception ex)
            {
                ShowError(
                    $"Unable to refresh reports: {ex.Message}");
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


            GenerateReportButton.IsEnabled =
                !loading;


            GeneratePdfButton.IsEnabled =
                !loading;


            ResetReportButton.IsEnabled =
                !loading;
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
    // DAILY RECORD DTO
    // =============================================================

    public class ReportDailyRecordDto
    {
        public long RecordId
        {
            get;
            set;
        }


        // =========================================================
        // NESTED EMPLOYEE OBJECT RETURNED BY API
        // =========================================================

        public ReportRecordEmployeeDto? Employee
        {
            get;
            set;
        }


        // =========================================================
        // RECORD DATA
        // =========================================================

        public DateTime RecordDate
        {
            get;
            set;
        }


        public string ActivityType
        {
            get;
            set;
        } = string.Empty;


        public string Reference
        {
            get;
            set;
        } = string.Empty;


        public int Quantity
        {
            get;
            set;
        }


        public decimal BusinessValue
        {
            get;
            set;
        }


        public int SecondaryMetric
        {
            get;
            set;
        }


        public string Status
        {
            get;
            set;
        } = string.Empty;


        public string BusinessImpact
        {
            get;
            set;
        } = string.Empty;


        public string Notes
        {
            get;
            set;
        } = string.Empty;


        public DateTime SubmittedAt
        {
            get;
            set;
        }


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


        public string BusinessValueDisplay =>
            $"R {BusinessValue:N2}";


        public string StatusDisplay =>
            string.IsNullOrWhiteSpace(
                Status)

                ? "Unknown"

                : Status;


        public string DateDisplay =>
            RecordDate.ToString(
                "dd MMM yyyy");


        public string DetailsDisplay
        {
            get
            {
                List<string> details =
                    new();


                details.Add(
                    $"Qty: {Quantity:N0}");


                if (SecondaryMetric != 0)
                {
                    details.Add(
                        $"Secondary: {SecondaryMetric:N0}");
                }


                if (!string.IsNullOrWhiteSpace(
                        BusinessImpact))
                {
                    details.Add(
                        $"Impact: {BusinessImpact}");
                }


                if (!string.IsNullOrWhiteSpace(
                        Notes))
                {
                    details.Add(
                        Notes);
                }


                return string.Join(
                    " • ",
                    details);
            }
        }
    }


    // =============================================================
    // NESTED EMPLOYEE DTO
    // =============================================================

    public class ReportRecordEmployeeDto
    {
        public long AccountId
        {
            get;
            set;
        }


        public string EmployeeId
        {
            get;
            set;
        } = string.Empty;


        public string FirstName
        {
            get;
            set;
        } = string.Empty;


        public string LastName
        {
            get;
            set;
        } = string.Empty;


        public string Department
        {
            get;
            set;
        } = string.Empty;


        public string DepartmentCode
        {
            get;
            set;
        } = string.Empty;
    }


    // =============================================================
    // EMPLOYEE OPTION
    // =============================================================

    public class ReportEmployeeOption
    {
        public long EmployeeAccountId
        {
            get;
            set;
        }


        public string EmployeeId
        {
            get;
            set;
        } = string.Empty;


        public string EmployeeName
        {
            get;
            set;
        } = string.Empty;


        public string Department
        {
            get;
            set;
        } = string.Empty;


        public string DisplayName
        {
            get
            {
                if (EmployeeAccountId == 0)
                {
                    return "All Employees";
                }


                if (string.IsNullOrWhiteSpace(
                        EmployeeId))
                {
                    return EmployeeName;
                }


                return
                    $"{EmployeeName} ({EmployeeId})";
            }
        }
    }


    // =============================================================
    // REPORT BREAKDOWN
    // =============================================================

    public class ReportBreakdownItem
    {
        public string Name
        {
            get;
            set;
        } = string.Empty;


        public int Activities
        {
            get;
            set;
        }


        public int Completed
        {
            get;
            set;
        }


        public double CompletionRate
        {
            get;
            set;
        }


        public decimal BusinessValue
        {
            get;
            set;
        }


        public string ActivitiesDisplay =>
            Activities.ToString(
                "N0");


        public string CompletedDisplay =>
            Completed.ToString(
                "N0");


        public string CompletionRateDisplay =>
            $"{CompletionRate:N1}%";


        public string BusinessValueDisplay =>
            $"R {BusinessValue:N2}";
    }
}