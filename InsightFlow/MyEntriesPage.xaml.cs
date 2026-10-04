using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InsightFlow
{
    public partial class MyEntriesPage : ContentPage
    {
        private const string ApiBaseUrl =
            "https://localhost:7212/";

        private readonly HttpClient _httpClient;

        private readonly List<MyDailyRecordDto> _records =
            new();

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public MyEntriesPage()
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

            StatusFilterPicker.SelectedIndex = 0;
        }


        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await ConfigureAuthenticationAsync();

            await LoadMyEntriesAsync();
        }


        // =========================================================
        // JWT AUTHENTICATION
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
        // LOAD CURRENT EMPLOYEE RECORDS
        // =========================================================

        private async Task LoadMyEntriesAsync()
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
                        "Unable to Load Records",
                        string.IsNullOrWhiteSpace(error)
                            ? "Your daily records could not be loaded."
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


                List<MyDailyRecordDto> records =
                    ParseDailyRecords(json);


                _records.Clear();

                _records.AddRange(records);


                UpdateSummary();

                ApplyFilters();
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
                    $"Unable to load your records: {ex.Message}",
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

        private static List<MyDailyRecordDto>
            ParseDailyRecords(
                string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<MyDailyRecordDto>();
            }


            using JsonDocument document =
                JsonDocument.Parse(json);


            JsonElement root =
                document.RootElement;


            // -----------------------------------------------------
            // FORMAT 1:
            // [ {...}, {...} ]
            // -----------------------------------------------------

            if (root.ValueKind ==
                JsonValueKind.Array)
            {
                return DeserializeArray(root);
            }


            if (root.ValueKind !=
                JsonValueKind.Object)
            {
                return new List<MyDailyRecordDto>();
            }


            // -----------------------------------------------------
            // FORMAT 2:
            // { "$values": [ ... ] }
            // -----------------------------------------------------

            if (TryGetArrayProperty(
                    root,
                    "$values",
                    out JsonElement valuesArray))
            {
                return DeserializeArray(
                    valuesArray);
            }


            // -----------------------------------------------------
            // FORMAT 3:
            // { "records": [ ... ] }
            // { "data": [ ... ] }
            // { "items": [ ... ] }
            // -----------------------------------------------------

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


                // ---------------------------------------------
                // FORMAT 4:
                // {
                //   "records": {
                //      "$values": [ ... ]
                //   }
                // }
                // ---------------------------------------------

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


            return new List<MyDailyRecordDto>();
        }


        // =========================================================
        // DESERIALIZE JSON ARRAY
        // =========================================================

        private static List<MyDailyRecordDto>
            DeserializeArray(
                JsonElement array)
        {
            try
            {
                return JsonSerializer.Deserialize<
                           List<MyDailyRecordDto>>(
                           array.GetRawText(),
                           JsonOptions)
                       ?? new List<MyDailyRecordDto>();
            }
            catch
            {
                return new List<MyDailyRecordDto>();
            }
        }


        // =========================================================
        // FIND ARRAY PROPERTY
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
        // SUMMARY
        // =========================================================

        private void UpdateSummary()
        {
            int total =
                _records.Count;


            int completed =
                _records.Count(
                    record =>
                        string.Equals(
                            record.Status,
                            "Completed",
                            StringComparison.OrdinalIgnoreCase));


            int inProgress =
                _records.Count(
                    record =>
                        string.Equals(
                            record.Status,
                            "In Progress",
                            StringComparison.OrdinalIgnoreCase));


            decimal totalBusinessValue =
                _records.Sum(
                    record =>
                        record.BusinessValue);


            TotalEntriesLabel.Text =
                total.ToString();


            CompletedEntriesLabel.Text =
                completed.ToString();


            InProgressEntriesLabel.Text =
                inProgress.ToString();


            TotalBusinessValueLabel.Text =
                $"R {totalBusinessValue:N2}";
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
        // STATUS FILTER
        // =========================================================

        private void OnStatusFilterChanged(
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


            string selectedStatus =
                StatusFilterPicker
                    .SelectedItem?
                    .ToString()
                ?? "All";


            IEnumerable<MyDailyRecordDto> result =
                _records;


            // SEARCH

            if (!string.IsNullOrWhiteSpace(
                    search))
            {
                result =
                    result.Where(
                        record =>
                            record.ActivityType.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)

                            ||

                            record.Reference.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)

                            ||

                            record.Notes.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)

                            ||

                            record.BusinessImpact.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase));
            }


            // STATUS

            if (!string.Equals(
                    selectedStatus,
                    "All",
                    StringComparison.OrdinalIgnoreCase))
            {
                result =
                    result.Where(
                        record =>
                            string.Equals(
                                record.Status,
                                selectedStatus,
                                StringComparison.OrdinalIgnoreCase));
            }


            List<MyDailyRecordDto> displayRecords =
                result
                    .OrderByDescending(
                        record =>
                            record.RecordDate)
                    .ThenByDescending(
                        record =>
                            record.SubmittedAt)
                    .ToList();


            EntriesCollectionView.ItemsSource =
                displayRecords;


            RecordCountLabel.Text =
                displayRecords.Count == 1
                    ? "1 record"
                    : $"{displayRecords.Count} records";
        }


        // =========================================================
        // REFRESH
        // =========================================================

        private async void OnRefreshClicked(
            object sender,
            EventArgs e)
        {
            await LoadMyEntriesAsync();
        }


        // =========================================================
        // LOADING
        // =========================================================

        private void SetLoading(
            bool isLoading)
        {
            LoadingIndicator.IsVisible =
                isLoading;

            LoadingIndicator.IsRunning =
                isLoading;
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

    public class MyDailyRecordDto
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


        // =========================================================
        // DISPLAY PROPERTIES
        // =========================================================

        public string DateDisplay =>
            RecordDate.ToString(
                "dd MMM yyyy");


        public string BusinessValueDisplay =>
            $"R {BusinessValue:N2}";


        public string ReferenceDisplay =>
            string.IsNullOrWhiteSpace(
                Reference)
                ? "No reference"
                : $"Reference: {Reference}";


        public string QuantityDisplay =>
            $"Qty: {Quantity}";


        public string NotesDisplay =>
            string.IsNullOrWhiteSpace(
                Notes)
                ? "No additional notes"
                : Notes;
    }
}