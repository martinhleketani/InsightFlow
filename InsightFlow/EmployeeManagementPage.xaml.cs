using Microsoft.Maui.Storage;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace InsightFlow
{
    public partial class EmployeeManagementPage : ContentPage
    {
        private const string ApiBaseUrl =
            "https://localhost:7212/";

        private readonly HttpClient _httpClient;

        private readonly List<EmployeeDto> _employees =
            new();

        private long? _selectedAccountId;

        private bool _editingEmployee;

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public EmployeeManagementPage()
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

            ClearForm();
        }


        // =========================================================
        // PAGE APPEARING
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await ConfigureAuthenticationAsync();

            await LoadEmployeesAsync();
        }


        // =========================================================
        // AUTHENTICATION
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
        // LOAD EMPLOYEES
        // =========================================================

        private async Task LoadEmployeesAsync()
        {
            try
            {
                SetLoading(true);

                HideValidation();

                await ConfigureAuthenticationAsync();


                using HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        "api/Employees");


                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    ShowValidation(
                        "Your session has expired. Please sign in again.");

                    return;
                }


                if (response.StatusCode ==
                    HttpStatusCode.Forbidden)
                {
                    ShowValidation(
                        "Administrator access is required to manage employees.");

                    return;
                }


                if (!response.IsSuccessStatusCode)
                {
                    string message =
                        await ReadErrorMessageAsync(
                            response);

                    ShowValidation(
                        string.IsNullOrWhiteSpace(message)
                            ? "Unable to load employees."
                            : message);

                    return;
                }


                // =================================================
                // READ THE RAW API RESPONSE
                // =================================================

                string json =
                    await response.Content
                        .ReadAsStringAsync();


                // =================================================
                // PARSE ARRAY OR WRAPPED RESPONSE
                // =================================================

                List<EmployeeDto> employees =
                    ParseEmployees(json);


                _employees.Clear();

                _employees.AddRange(
                    employees);


                ApplySearchFilter();
            }
            catch (HttpRequestException)
            {
                ShowValidation(
                    "Unable to connect to the InsightFlow API. Make sure the API is running.");
            }
            catch (TaskCanceledException)
            {
                ShowValidation(
                    "The request to the InsightFlow API timed out.");
            }
            catch (JsonException ex)
            {
                ShowValidation(
                    $"The API returned employee data in an unexpected format: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowValidation(
                    $"Unable to load employees: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }


        // =========================================================
        // PARSE EMPLOYEES
        // =========================================================

        private static List<EmployeeDto>
            ParseEmployees(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<EmployeeDto>();
            }


            using JsonDocument document =
                JsonDocument.Parse(json);


            JsonElement root =
                document.RootElement;


            // =====================================================
            // FORMAT 1:
            // [
            //     { employee },
            //     { employee }
            // ]
            // =====================================================

            if (root.ValueKind ==
                JsonValueKind.Array)
            {
                return DeserializeEmployeeArray(
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
                    return DeserializeEmployeeArray(
                        values);
                }


                // =================================================
                // FORMAT 3:
                // { "employees": [...] }
                // =================================================

                if (TryGetArrayProperty(
                        root,
                        "employees",
                        out JsonElement employees))
                {
                    return DeserializeEmployeeArray(
                        employees);
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
                    return DeserializeEmployeeArray(
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
                    return DeserializeEmployeeArray(
                        items);
                }


                // =================================================
                // FORMAT 6:
                //
                // {
                //     "employees":
                //     {
                //         "$values": [...]
                //     }
                // }
                // =================================================

                if (TryGetPropertyIgnoreCase(
                        root,
                        "employees",
                        out JsonElement employeesObject)

                    &&

                    employeesObject.ValueKind ==
                    JsonValueKind.Object

                    &&

                    TryGetArrayProperty(
                        employeesObject,
                        "$values",
                        out JsonElement nestedEmployees))
                {
                    return DeserializeEmployeeArray(
                        nestedEmployees);
                }


                // =================================================
                // FORMAT 7:
                //
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
                        out JsonElement nestedData))
                {
                    return DeserializeEmployeeArray(
                        nestedData);
                }


                // =================================================
                // FORMAT 8:
                //
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
                    return DeserializeEmployeeArray(
                        nestedItems);
                }
            }


            throw new JsonException(
                "Employees response did not contain a supported employee collection.");
        }


        // =========================================================
        // DESERIALIZE EMPLOYEE ARRAY
        // =========================================================

        private static List<EmployeeDto>
            DeserializeEmployeeArray(
                JsonElement array)
        {
            if (array.ValueKind !=
                JsonValueKind.Array)
            {
                return new List<EmployeeDto>();
            }


            List<EmployeeDto>? employees =
                JsonSerializer.Deserialize<
                    List<EmployeeDto>>(
                        array.GetRawText(),
                        JsonOptions);


            return employees ??
                   new List<EmployeeDto>();
        }


        // =========================================================
        // ARRAY PROPERTY
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


            IEnumerable<EmployeeDto> result =
                _employees;


            if (!string.IsNullOrWhiteSpace(
                    search))
            {
                result =
                    _employees.Where(
                        employee =>

                            ContainsText(
                                employee.FullName,
                                search)

                            ||

                            ContainsText(
                                employee.EmployeeId,
                                search)

                            ||

                            ContainsText(
                                employee.CompanyEmail,
                                search)

                            ||

                            ContainsText(
                                employee.Department,
                                search)

                            ||

                            ContainsText(
                                employee.JobTitle,
                                search)

                            ||

                            ContainsText(
                                employee.Role,
                                search));
            }


            List<EmployeeDto> displayList =
                result
                    .OrderBy(
                        employee =>
                            employee.FirstName)

                    .ThenBy(
                        employee =>
                            employee.LastName)

                    .ToList();


            EmployeesCollectionView.ItemsSource =
                displayList;


            EmployeeCountLabel.Text =
                displayList.Count == 1
                    ? "1 employee"
                    : $"{displayList.Count} employees";
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
        // EMPLOYEE SELECTED
        // =========================================================

        private void OnEmployeeSelected(
            object sender,
            SelectionChangedEventArgs e)
        {
            EmployeeDto? employee =
                e.CurrentSelection
                    .FirstOrDefault()
                    as EmployeeDto;


            if (employee == null)
            {
                return;
            }


            LoadEmployeeIntoForm(
                employee);
        }


        private void LoadEmployeeIntoForm(
            EmployeeDto employee)
        {
            _editingEmployee =
                true;


            _selectedAccountId =
                employee.AccountId;


            FormTitleLabel.Text =
                "Edit Employee";


            SaveButton.Text =
                "UPDATE EMPLOYEE";


            PasswordLabel.IsVisible =
                false;


            PasswordBorder.IsVisible =
                false;


            PasswordResetSection.IsVisible =
                true;


            EmployeeIdEntry.Text =
                employee.EmployeeId;


            FirstNameEntry.Text =
                employee.FirstName;


            LastNameEntry.Text =
                employee.LastName;


            CompanyEmailEntry.Text =
                employee.CompanyEmail;


            JobTitleEntry.Text =
                employee.JobTitle;


            ActiveSwitch.IsToggled =
                employee.IsActive;


            SelectPickerValue(
                DepartmentPicker,
                employee.Department);


            SelectPickerValue(
                RolePicker,
                employee.Role);


            HideValidation();
        }


        // =========================================================
        // SAVE
        // =========================================================

        private async void OnSaveClicked(
            object sender,
            EventArgs e)
        {
            HideValidation();


            if (!ValidateForm())
            {
                return;
            }


            if (_editingEmployee)
            {
                await UpdateEmployeeAsync();
            }
            else
            {
                await CreateEmployeeAsync();
            }
        }


        // =========================================================
        // CREATE EMPLOYEE
        // =========================================================

        private async Task CreateEmployeeAsync()
        {
            try
            {
                SetLoading(true);

                await ConfigureAuthenticationAsync();


                CreateEmployeeRequest request =
                    new CreateEmployeeRequest
                    {
                        EmployeeId =
                            EmployeeIdEntry.Text!
                                .Trim(),

                        FirstName =
                            FirstNameEntry.Text!
                                .Trim(),

                        LastName =
                            LastNameEntry.Text!
                                .Trim(),

                        CompanyEmail =
                            CompanyEmailEntry.Text!
                                .Trim()
                                .ToLowerInvariant(),

                        Password =
                            PasswordEntry.Text!,

                        DepartmentId =
                            GetDepartmentId(),

                        JobTitle =
                            JobTitleEntry.Text!
                                .Trim(),

                        Role =
                            RolePicker
                                .SelectedItem!
                                .ToString()!,

                        IsActive =
                            ActiveSwitch.IsToggled
                    };


                using HttpResponseMessage response =
                    await _httpClient
                        .PostAsJsonAsync(
                            "api/Employees",
                            request);


                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    ShowValidation(
                        "Your session has expired. Please sign in again.");

                    return;
                }


                if (response.StatusCode ==
                    HttpStatusCode.Forbidden)
                {
                    ShowValidation(
                        "Administrator access is required to create employees.");

                    return;
                }


                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await ReadErrorMessageAsync(
                            response);


                    ShowValidation(
                        string.IsNullOrWhiteSpace(error)
                            ? "Unable to create employee."
                            : error);


                    return;
                }


                await DisplayAlert(
                    "Employee Created",
                    "The employee account was created successfully.",
                    "OK");


                ClearForm();

                await LoadEmployeesAsync();
            }
            catch (HttpRequestException)
            {
                ShowValidation(
                    "Unable to connect to the InsightFlow API.");
            }
            catch (TaskCanceledException)
            {
                ShowValidation(
                    "The request to the InsightFlow API timed out.");
            }
            catch (Exception ex)
            {
                ShowValidation(
                    $"Unable to create employee: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }


        // =========================================================
        // UPDATE EMPLOYEE
        // =========================================================

        private async Task UpdateEmployeeAsync()
        {
            if (_selectedAccountId == null)
            {
                ShowValidation(
                    "Select an employee first.");

                return;
            }


            try
            {
                SetLoading(true);

                await ConfigureAuthenticationAsync();


                UpdateEmployeeRequest request =
                    new UpdateEmployeeRequest
                    {
                        EmployeeId =
                            EmployeeIdEntry.Text!
                                .Trim(),

                        FirstName =
                            FirstNameEntry.Text!
                                .Trim(),

                        LastName =
                            LastNameEntry.Text!
                                .Trim(),

                        CompanyEmail =
                            CompanyEmailEntry.Text!
                                .Trim()
                                .ToLowerInvariant(),

                        DepartmentId =
                            GetDepartmentId(),

                        JobTitle =
                            JobTitleEntry.Text!
                                .Trim(),

                        Role =
                            RolePicker
                                .SelectedItem!
                                .ToString()!,

                        IsActive =
                            ActiveSwitch.IsToggled
                    };


                using HttpResponseMessage response =
                    await _httpClient
                        .PutAsJsonAsync(
                            $"api/Employees/{_selectedAccountId}",
                            request);


                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    ShowValidation(
                        "Your session has expired. Please sign in again.");

                    return;
                }


                if (response.StatusCode ==
                    HttpStatusCode.Forbidden)
                {
                    ShowValidation(
                        "Administrator access is required to update employees.");

                    return;
                }


                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await ReadErrorMessageAsync(
                            response);


                    ShowValidation(
                        string.IsNullOrWhiteSpace(error)
                            ? "Unable to update employee."
                            : error);


                    return;
                }


                await UpdateEmployeeStatusAsync(
                    _selectedAccountId.Value,
                    ActiveSwitch.IsToggled);


                await DisplayAlert(
                    "Employee Updated",
                    "The employee account was updated successfully.",
                    "OK");


                ClearForm();

                await LoadEmployeesAsync();
            }
            catch (HttpRequestException)
            {
                ShowValidation(
                    "Unable to connect to the InsightFlow API.");
            }
            catch (TaskCanceledException)
            {
                ShowValidation(
                    "The request to the InsightFlow API timed out.");
            }
            catch (Exception ex)
            {
                ShowValidation(
                    $"Unable to update employee: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }


        // =========================================================
        // STATUS
        // =========================================================

        private async Task UpdateEmployeeStatusAsync(
            long accountId,
            bool isActive)
        {
            EmployeeStatusRequest request =
                new EmployeeStatusRequest
                {
                    IsActive =
                        isActive
                };


            using HttpRequestMessage message =
                new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"api/Employees/{accountId}/status");


            message.Content =
                JsonContent.Create(
                    request);


            using HttpResponseMessage response =
                await _httpClient.SendAsync(
                    message);


            if (!response.IsSuccessStatusCode)
            {
                string error =
                    await ReadErrorMessageAsync(
                        response);


                throw new Exception(
                    string.IsNullOrWhiteSpace(error)
                        ? "Unable to update employee status."
                        : error);
            }
        }


        // =========================================================
        // PASSWORD
        // =========================================================

        private async void OnChangePasswordClicked(
            object sender,
            EventArgs e)
        {
            if (_selectedAccountId == null)
            {
                ShowValidation(
                    "Select an employee first.");

                return;
            }


            string password =
                NewPasswordEntry.Text
                ?? string.Empty;


            if (string.IsNullOrWhiteSpace(
                    password))
            {
                ShowValidation(
                    "Enter the employee's new password.");

                return;
            }


            if (password.Length < 8)
            {
                ShowValidation(
                    "Password must contain at least 8 characters.");

                return;
            }


            bool confirm =
                await DisplayAlert(
                    "Change Password",
                    "Change the password for this employee?",
                    "Change",
                    "Cancel");


            if (!confirm)
            {
                return;
            }


            try
            {
                SetLoading(true);

                await ConfigureAuthenticationAsync();


                ChangePasswordRequest request =
                    new ChangePasswordRequest
                    {
                        NewPassword =
                            password
                    };


                using HttpRequestMessage message =
                    new HttpRequestMessage(
                        HttpMethod.Patch,
                        $"api/Employees/{_selectedAccountId}/password");


                message.Content =
                    JsonContent.Create(
                        request);


                using HttpResponseMessage response =
                    await _httpClient.SendAsync(
                        message);


                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    ShowValidation(
                        "Your session has expired. Please sign in again.");

                    return;
                }


                if (response.StatusCode ==
                    HttpStatusCode.Forbidden)
                {
                    ShowValidation(
                        "Administrator access is required to change employee passwords.");

                    return;
                }


                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await ReadErrorMessageAsync(
                            response);


                    ShowValidation(
                        string.IsNullOrWhiteSpace(error)
                            ? "Unable to change password."
                            : error);


                    return;
                }


                NewPasswordEntry.Text =
                    string.Empty;


                await DisplayAlert(
                    "Password Changed",
                    "The employee password was changed successfully.",
                    "OK");
            }
            catch (HttpRequestException)
            {
                ShowValidation(
                    "Unable to connect to the InsightFlow API.");
            }
            catch (TaskCanceledException)
            {
                ShowValidation(
                    "The request to the InsightFlow API timed out.");
            }
            catch (Exception ex)
            {
                ShowValidation(
                    $"Unable to change password: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }


        // =========================================================
        // VALIDATION
        // =========================================================

        private bool ValidateForm()
        {
            string employeeId =
                EmployeeIdEntry.Text?.Trim()
                ?? string.Empty;


            string firstName =
                FirstNameEntry.Text?.Trim()
                ?? string.Empty;


            string lastName =
                LastNameEntry.Text?.Trim()
                ?? string.Empty;


            string email =
                CompanyEmailEntry.Text?.Trim()
                ?? string.Empty;


            string jobTitle =
                JobTitleEntry.Text?.Trim()
                ?? string.Empty;


            if (string.IsNullOrWhiteSpace(
                    employeeId))
            {
                ShowValidation(
                    "Employee ID is required.");

                return false;
            }


            if (string.IsNullOrWhiteSpace(
                    firstName))
            {
                ShowValidation(
                    "First name is required.");

                return false;
            }


            if (string.IsNullOrWhiteSpace(
                    lastName))
            {
                ShowValidation(
                    "Last name is required.");

                return false;
            }


            if (string.IsNullOrWhiteSpace(
                    email))
            {
                ShowValidation(
                    "Company email is required.");

                return false;
            }


            if (!email.EndsWith(
                    "@insightflow.co.za",
                    StringComparison.OrdinalIgnoreCase))
            {
                ShowValidation(
                    "Use an @insightflow.co.za company email.");

                return false;
            }


            if (DepartmentPicker.SelectedIndex < 0)
            {
                ShowValidation(
                    "Select a department.");

                return false;
            }


            if (string.IsNullOrWhiteSpace(
                    jobTitle))
            {
                ShowValidation(
                    "Job title is required.");

                return false;
            }


            if (RolePicker.SelectedIndex < 0)
            {
                ShowValidation(
                    "Select a system role.");

                return false;
            }


            if (!_editingEmployee)
            {
                string password =
                    PasswordEntry.Text
                    ?? string.Empty;


                if (string.IsNullOrWhiteSpace(
                        password))
                {
                    ShowValidation(
                        "Initial password is required.");

                    return false;
                }


                if (password.Length < 8)
                {
                    ShowValidation(
                        "Password must contain at least 8 characters.");

                    return false;
                }
            }


            return true;
        }


        // =========================================================
        // DEPARTMENT
        // =========================================================

        private int GetDepartmentId()
        {
            string department =
                DepartmentPicker
                    .SelectedItem?
                    .ToString()
                ?? string.Empty;


            return department switch
            {
                "Sales" => 1,

                "Finance" => 2,

                "Human Resources" => 3,

                "Information Technology" => 4,

                "Marketing" => 5,

                "Operations" => 6,

                "Customer Service" => 7,

                "Procurement" => 8,

                "Management" => 9,

                _ => 0
            };
        }


        private void OnDepartmentChanged(
            object sender,
            EventArgs e)
        {
            /*
             * Department is selected by the administrator.
             *
             * Employee IDs are not generated here because
             * the backend/database remains the authority.
             */
        }


        // =========================================================
        // PICKER
        // =========================================================

        private static void SelectPickerValue(
            Picker picker,
            string value)
        {
            for (int i = 0;
                 i < picker.Items.Count;
                 i++)
            {
                if (string.Equals(
                        picker.Items[i],
                        value,
                        StringComparison.OrdinalIgnoreCase))
                {
                    picker.SelectedIndex =
                        i;

                    return;
                }
            }


            picker.SelectedIndex =
                -1;
        }


        // =========================================================
        // REFRESH
        // =========================================================

        private async void OnRefreshClicked(
            object sender,
            EventArgs e)
        {
            await LoadEmployeesAsync();
        }


        // =========================================================
        // CLEAR
        // =========================================================

        private void OnClearClicked(
            object sender,
            EventArgs e)
        {
            ClearForm();
        }


        private void ClearForm()
        {
            _selectedAccountId =
                null;


            _editingEmployee =
                false;


            FormTitleLabel.Text =
                "Add Employee";


            SaveButton.Text =
                "CREATE EMPLOYEE";


            EmployeeIdEntry.Text =
                string.Empty;


            FirstNameEntry.Text =
                string.Empty;


            LastNameEntry.Text =
                string.Empty;


            CompanyEmailEntry.Text =
                string.Empty;


            JobTitleEntry.Text =
                string.Empty;


            PasswordEntry.Text =
                string.Empty;


            NewPasswordEntry.Text =
                string.Empty;


            DepartmentPicker.SelectedIndex =
                -1;


            RolePicker.SelectedIndex =
                0;


            ActiveSwitch.IsToggled =
                true;


            PasswordLabel.IsVisible =
                true;


            PasswordBorder.IsVisible =
                true;


            PasswordResetSection.IsVisible =
                false;


            EmployeesCollectionView.SelectedItem =
                null;


            HideValidation();
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


            SaveButton.IsEnabled =
                !loading;
        }


        // =========================================================
        // VALIDATION DISPLAY
        // =========================================================

        private void ShowValidation(
            string message)
        {
            ValidationLabel.Text =
                message;


            ValidationBorder.IsVisible =
                true;
        }


        private void HideValidation()
        {
            ValidationLabel.Text =
                string.Empty;


            ValidationBorder.IsVisible =
                false;
        }


        // =========================================================
        // ERROR RESPONSE
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
                    JsonDocument.Parse(
                        json);


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
    // EMPLOYEE DTO
    // =============================================================

    public class EmployeeDto
    {
        public long AccountId { get; set; }


        public string EmployeeId { get; set; } =
            string.Empty;


        public string FirstName { get; set; } =
            string.Empty;


        public string LastName { get; set; } =
            string.Empty;


        public string CompanyEmail { get; set; } =
            string.Empty;


        public int DepartmentId { get; set; }


        public string Department { get; set; } =
            string.Empty;


        public string DepartmentCode { get; set; } =
            string.Empty;


        public string JobTitle { get; set; } =
            string.Empty;


        public string Role { get; set; } =
            string.Empty;


        public bool IsActive { get; set; }


        public DateTime DateCreated { get; set; }


        public string FullName =>
            $"{FirstName} {LastName}".Trim();


        public string StatusText =>
            IsActive
                ? "ACTIVE"
                : "INACTIVE";


        public string EmployeeSummary =>
            $"{EmployeeId} • {CompanyEmail}";


        public string DepartmentSummary =>
            $"{Department} • {JobTitle} • {Role}";
    }


    // =============================================================
    // CREATE REQUEST
    // =============================================================

    public class CreateEmployeeRequest
    {
        public string EmployeeId { get; set; } =
            string.Empty;


        public string FirstName { get; set; } =
            string.Empty;


        public string LastName { get; set; } =
            string.Empty;


        public string CompanyEmail { get; set; } =
            string.Empty;


        public string Password { get; set; } =
            string.Empty;


        public int DepartmentId { get; set; }


        public string JobTitle { get; set; } =
            string.Empty;


        public string Role { get; set; } =
            "Employee";


        public bool IsActive { get; set; } =
            true;
    }


    // =============================================================
    // UPDATE REQUEST
    // =============================================================

    public class UpdateEmployeeRequest
    {
        public string EmployeeId { get; set; } =
            string.Empty;


        public string FirstName { get; set; } =
            string.Empty;


        public string LastName { get; set; } =
            string.Empty;


        public string CompanyEmail { get; set; } =
            string.Empty;


        public int DepartmentId { get; set; }


        public string JobTitle { get; set; } =
            string.Empty;


        public string Role { get; set; } =
            string.Empty;


        public bool IsActive { get; set; }
    }


    // =============================================================
    // STATUS REQUEST
    // =============================================================

    public class EmployeeStatusRequest
    {
        public bool IsActive { get; set; }
    }


    // =============================================================
    // PASSWORD REQUEST
    // =============================================================

    public class ChangePasswordRequest
    {
        public string NewPassword { get; set; } =
            string.Empty;
    }
}