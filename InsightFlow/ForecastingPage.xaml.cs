using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InsightFlow
{
    public partial class ForecastingPage : ContentPage
    {
        private const string ApiBaseUrl =
            "https://localhost:7212/";

        private readonly HttpClient _httpClient;

        private readonly List<ForecastDailyRecordDto>
            _records = new();

        private List<MonthlyForecastData>
            _monthlyHistory = new();

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ForecastingPage()
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

            DepartmentPicker.SelectedIndex = 0;

            ForecastMethodPicker.SelectedIndex = 0;

            ResetPage();
        }


        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadForecastDataAsync();
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

        private async Task LoadForecastDataAsync()
        {
            try
            {
                SetLoading(true);

                HideError();

                _records.Clear();

                _monthlyHistory.Clear();

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
                        "Administrator or Manager access is required to view forecasting.");

                    return;
                }


                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await ReadErrorMessageAsync(
                            response);

                    ShowError(
                        string.IsNullOrWhiteSpace(error)
                            ? "Unable to load historical records."
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
                // PARSE DIRECT OR WRAPPED API RESPONSE
                // =================================================

                List<ForecastDailyRecordDto> records =
                    ParseForecastRecords(json);


                _records.AddRange(
                    records);


                CalculateForecast();


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
                    $"The API returned historical data in an unexpected format: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError(
                    $"Unable to calculate forecast: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }


        // =========================================================
        // PARSE FORECAST RECORDS
        // =========================================================

        private static List<ForecastDailyRecordDto>
            ParseForecastRecords(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<ForecastDailyRecordDto>();
            }


            using JsonDocument document =
                JsonDocument.Parse(json);


            JsonElement root =
                document.RootElement;


            // Direct array
            if (root.ValueKind ==
                JsonValueKind.Array)
            {
                return DeserializeRecordArray(
                    root);
            }


            if (root.ValueKind ==
                JsonValueKind.Object)
            {
                // { "$values": [...] }
                if (TryGetArrayProperty(
                        root,
                        "$values",
                        out JsonElement values))
                {
                    return DeserializeRecordArray(
                        values);
                }


                // { "records": [...] }
                if (TryGetArrayProperty(
                        root,
                        "records",
                        out JsonElement records))
                {
                    return DeserializeRecordArray(
                        records);
                }


                // { "data": [...] }
                if (TryGetArrayProperty(
                        root,
                        "data",
                        out JsonElement data))
                {
                    return DeserializeRecordArray(
                        data);
                }


                // { "items": [...] }
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
                "DailyRecords response did not contain a supported historical-record collection.");
        }


        // =========================================================
        // DESERIALIZE RECORD ARRAY
        // =========================================================

        private static List<ForecastDailyRecordDto>
            DeserializeRecordArray(
                JsonElement array)
        {
            if (array.ValueKind !=
                JsonValueKind.Array)
            {
                return new List<ForecastDailyRecordDto>();
            }


            List<ForecastDailyRecordDto>? records =
                JsonSerializer.Deserialize<
                    List<ForecastDailyRecordDto>>(
                        array.GetRawText(),
                        JsonOptions);


            return records ??
                   new List<ForecastDailyRecordDto>();
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
        // CALCULATE FORECAST
        // =========================================================

        private void CalculateForecast()
        {
            HideError();


            IEnumerable<ForecastDailyRecordDto>
                selectedRecords =
                    GetSelectedRecords();


            List<ForecastDailyRecordDto>
                records =
                    selectedRecords

                        .Where(
                            record =>
                                record.RecordDate != default)

                        .ToList();


            // =====================================================
            // GROUP ACTUAL DATA BY MONTH
            // =====================================================

            _monthlyHistory =
                records

                    .GroupBy(
                        record =>
                            new
                            {
                                record.RecordDate.Year,
                                record.RecordDate.Month
                            })

                    .Select(
                        group =>
                            new MonthlyForecastData
                            {
                                Year =
                                    group.Key.Year,

                                Month =
                                    group.Key.Month,

                                ActivityCount =
                                    group.Count(),

                                Quantity =
                                    group.Sum(
                                        record =>
                                            (long)record.Quantity),

                                BusinessValue =
                                    group.Sum(
                                        record =>
                                            record.BusinessValue),

                                Completed =
                                    group.Count(
                                        record =>
                                            string.Equals(
                                                record.Status?.Trim(),
                                                "Completed",
                                                StringComparison.OrdinalIgnoreCase))
                            })

                    .OrderBy(
                        month =>
                            month.Year)

                    .ThenBy(
                        month =>
                            month.Month)

                    .ToList();


            MonthlyHistoryCollectionView.ItemsSource =
                _monthlyHistory;


            CalculateHistoricalMetrics(
                records);


            CalculateMovingAverageForecast();
        }


        // =========================================================
        // GET SELECTED RECORDS
        // =========================================================

        private IEnumerable<ForecastDailyRecordDto>
            GetSelectedRecords()
        {
            string department =
                DepartmentPicker
                    .SelectedItem?
                    .ToString()
                ?? "All Departments";


            if (string.Equals(
                    department,
                    "All Departments",
                    StringComparison.OrdinalIgnoreCase))
            {
                return _records;
            }


            return _records.Where(
                record =>
                    string.Equals(
                        record.Department?.Trim(),
                        department,
                        StringComparison.OrdinalIgnoreCase));
        }


        // =========================================================
        // HISTORICAL METRICS
        // =========================================================

        private void CalculateHistoricalMetrics(
            List<ForecastDailyRecordDto> records)
        {
            int monthCount =
                _monthlyHistory.Count;


            decimal historicalValue =
                records.Sum(
                    record =>
                        record.BusinessValue);


            int activities =
                records.Count;


            decimal averageMonthlyValue =
                monthCount == 0
                    ? 0
                    : historicalValue /
                      monthCount;


            HistoryMonthsLabel.Text =
                monthCount == 1
                    ? "1 month"
                    : $"{monthCount} months";


            HistoricalValueLabel.Text =
                $"R {historicalValue:N2}";


            HistoricalActivitiesLabel.Text =
                activities.ToString("N0");


            AverageMonthlyValueLabel.Text =
                $"R {averageMonthlyValue:N2}";


            if (monthCount == 0)
            {
                LatestMonthValueLabel.Text =
                    "R 0.00";

                LatestMonthLabel.Text =
                    "No historical month";

                return;
            }


            MonthlyForecastData latest =
                _monthlyHistory.Last();


            LatestMonthValueLabel.Text =
                $"R {latest.BusinessValue:N2}";


            LatestMonthLabel.Text =
                latest.MonthDisplay;
        }


        // =========================================================
        // MOVING AVERAGE FORECAST
        // =========================================================

        private void CalculateMovingAverageForecast()
        {
            int period =
                GetForecastPeriod();


            string methodName =
                period == 2
                    ? "2-Month Moving Average"
                    : "3-Month Moving Average";


            MethodDisplayLabel.Text =
                methodName;


            ForecastExplanationLabel.Text =
                $"The next month forecast is calculated using the average of the latest {period} months of historical data.";


            if (_monthlyHistory.Count < period)
            {
                ForecastValueLabel.Text =
                    "R 0.00";

                ForecastActivityLabel.Text =
                    "0";

                ForecastMonthLabel.Text =
                    $"Need at least {period} months";

                ForecastDetailValueLabel.Text =
                    "R 0.00";

                ForecastDetailActivityLabel.Text =
                    "0";

                MonthsUsedLabel.Text =
                    $"0 of {period}";

                return;
            }


            List<MonthlyForecastData> recentMonths =
                _monthlyHistory
                    .TakeLast(period)
                    .ToList();


            decimal forecastBusinessValue =
                recentMonths.Average(
                    month =>
                        month.BusinessValue);


            double forecastActivitiesRaw =
                recentMonths.Average(
                    month =>
                        (double)month.ActivityCount);


            int forecastActivities =
                (int)Math.Round(
                    forecastActivitiesRaw,
                    MidpointRounding.AwayFromZero);


            MonthlyForecastData latest =
                _monthlyHistory.Last();


            DateTime nextMonth =
                new DateTime(
                    latest.Year,
                    latest.Month,
                    1)
                .AddMonths(1);


            ForecastValueLabel.Text =
                $"R {forecastBusinessValue:N2}";


            ForecastActivityLabel.Text =
                forecastActivities.ToString("N0");


            ForecastMonthLabel.Text =
                nextMonth.ToString(
                    "MMMM yyyy");


            ForecastDetailValueLabel.Text =
                $"R {forecastBusinessValue:N2}";


            ForecastDetailActivityLabel.Text =
                forecastActivities.ToString("N0");


            MonthsUsedLabel.Text =
                period.ToString();
        }


        // =========================================================
        // FORECAST PERIOD
        // =========================================================

        private int GetForecastPeriod()
        {
            string selected =
                ForecastMethodPicker
                    .SelectedItem?
                    .ToString()
                ?? "3-Month Moving Average";


            if (selected.StartsWith(
                    "2-",
                    StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }


            return 3;
        }


        // =========================================================
        // DEPARTMENT CHANGE
        // =========================================================

        private void OnDepartmentChanged(
            object sender,
            EventArgs e)
        {
            if (_records.Count > 0)
            {
                CalculateForecast();
            }
        }


        // =========================================================
        // METHOD CHANGE
        // =========================================================

        private void OnForecastMethodChanged(
            object sender,
            EventArgs e)
        {
            if (_records.Count > 0)
            {
                CalculateForecast();
            }
        }


        // =========================================================
        // CALCULATE BUTTON
        // =========================================================

        private void OnCalculateClicked(
            object sender,
            EventArgs e)
        {
            CalculateForecast();
        }


        // =========================================================
        // RESET
        // =========================================================

        private void ResetPage()
        {
            ResetMetrics();

            MonthlyHistoryCollectionView.ItemsSource =
                null;

            LastUpdatedLabel.Text =
                "Waiting for historical records";
        }


        private void ResetMetrics()
        {
            HistoryMonthsLabel.Text =
                "0 months";


            LatestMonthValueLabel.Text =
                "R 0.00";


            LatestMonthLabel.Text =
                "No historical month";


            ForecastValueLabel.Text =
                "R 0.00";


            ForecastMonthLabel.Text =
                "Insufficient history";


            ForecastActivityLabel.Text =
                "0";


            HistoricalValueLabel.Text =
                "R 0.00";


            HistoricalActivitiesLabel.Text =
                "0";


            AverageMonthlyValueLabel.Text =
                "R 0.00";


            MethodDisplayLabel.Text =
                "3-Month Moving Average";


            MonthsUsedLabel.Text =
                "0";


            ForecastDetailValueLabel.Text =
                "R 0.00";


            ForecastDetailActivityLabel.Text =
                "0";


            ForecastExplanationLabel.Text =
                "The next month forecast is calculated using the average of the latest three months of historical data.";
        }


        // =========================================================
        // REFRESH
        // =========================================================

        private async void OnRefreshClicked(
            object sender,
            EventArgs e)
        {
            await LoadForecastDataAsync();
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
    // DAILY RECORD DTO
    // =============================================================

    public class ForecastDailyRecordDto
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
    }


    // =============================================================
    // MONTHLY FORECAST DATA
    // =============================================================

    public class MonthlyForecastData
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public int ActivityCount { get; set; }

        public long Quantity { get; set; }

        public decimal BusinessValue { get; set; }

        public int Completed { get; set; }


        public string MonthDisplay
        {
            get
            {
                if (Year <= 0 ||
                    Month < 1 ||
                    Month > 12)
                {
                    return string.Empty;
                }


                return new DateTime(
                        Year,
                        Month,
                        1)
                    .ToString(
                        "MMMM yyyy");
            }
        }


        public string ActivityCountDisplay =>
            ActivityCount.ToString("N0");


        public string QuantityDisplay =>
            Quantity.ToString("N0");


        public string BusinessValueDisplay =>
            $"R {BusinessValue:N2}";


        public string CompletedDisplay =>
            Completed.ToString("N0");
    }
}