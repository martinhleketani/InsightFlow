using InsightFlow.Models;
using InsightFlow.Services;

namespace InsightFlow
{
    public partial class DailyEntryPage : ContentPage
    {
        private readonly EmployeeAccount currentEmployee;
        private readonly ApiService _apiService;
        private readonly DailyRecordService _dailyRecordService;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public DailyEntryPage(EmployeeAccount employee)
        {
            InitializeComponent();

            currentEmployee = employee
                ?? throw new ArgumentNullException(nameof(employee));

            _apiService = new ApiService();

            _dailyRecordService =
                new DailyRecordService(_apiService);

            LoadEmployeeInformation();

            ConfigureDepartmentForm();

            UpdateSelectionSummary();
        }


        // =========================================================
        // PAGE APPEARING / RESTORE LOGIN SESSION
        // =========================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            bool restored =
                await _apiService
                    .RestoreAuthorizationTokenAsync();

            if (!restored)
            {
                ShowValidation(
                    "Your login session could not be restored. Please sign in again.");
            }
        }


        // =========================================================
        // EMPLOYEE INFORMATION
        // =========================================================

        private void LoadEmployeeInformation()
        {
            EmployeeNameLabel.Text =
                currentEmployee.FullName;

            EmployeeIdLabel.Text =
                currentEmployee.EmployeeId;

            DepartmentLabel.Text =
                currentEmployee.Department;

            // Default to today.
            EntryDatePicker.Date =
                DateTime.Today;

            // Prevent future records.
            EntryDatePicker.MaximumDate =
                DateTime.Today;

            // Allow historical records.
            EntryDatePicker.MinimumDate =
                new DateTime(2020, 1, 1);
        }


        // =========================================================
        // CONFIGURE DEPARTMENT FORM
        // =========================================================

        private void ConfigureDepartmentForm()
        {
            ActivityTypePicker.Items.Clear();

            string department =
                currentEmployee.Department?.Trim()
                ?? string.Empty;

            switch (department)
            {
                case "Sales":
                    ConfigureSales();
                    break;

                case "Information Technology":
                    ConfigureInformationTechnology();
                    break;

                case "Finance":
                    ConfigureFinance();
                    break;

                case "Human Resources":
                    ConfigureHumanResources();
                    break;

                case "Marketing":
                    ConfigureMarketing();
                    break;

                case "Operations":
                    ConfigureOperations();
                    break;

                case "Customer Service":
                    ConfigureCustomerService();
                    break;

                case "Procurement":
                    ConfigureProcurement();
                    break;

                case "Management":
                    ConfigureManagement();
                    break;

                default:
                    ConfigureGeneral();
                    break;
            }

            ActivityTypePicker.SelectedIndex = -1;

            StatusPicker.SelectedIndex = -1;

            ImpactPicker.SelectedIndex = -1;

            UpdateSelectionSummary();
        }


        // =========================================================
        // SALES
        // =========================================================

        private void ConfigureSales()
        {
            FormTitleLabel.Text =
                "Sales Daily Activity";

            FormDescriptionLabel.Text =
                "Record your sales and customer activity.";

            AddActivityItems(
                "Product Sale",
                "Service Sale",
                "Customer Order",
                "Customer Follow-up",
                "Quotation",
                "Other");

            PrimaryFieldLabel.Text =
                "Product / Service *";

            PrimaryFieldEntry.Placeholder =
                "Enter product or service";

            QuantityFieldLabel.Text =
                "Units / Transactions";

            QuantityFieldEntry.Placeholder =
                "Enter number of units or transactions";

            MonetaryValueLabel.Text =
                "Sales Revenue (R)";

            MonetaryValueEntry.Placeholder =
                "Enter sales value";

            SecondaryMetricLabel.Text =
                "Customers Served";

            SecondaryMetricEntry.Placeholder =
                "Enter number of customers";
        }


        // =========================================================
        // INFORMATION TECHNOLOGY
        // =========================================================

        private void ConfigureInformationTechnology()
        {
            FormTitleLabel.Text =
                "IT Daily Activity";

            FormDescriptionLabel.Text =
                "Record technical support, development and system activity.";

            AddActivityItems(
                "Technical Support",
                "System Maintenance",
                "Software Development",
                "Network Support",
                "Security Task",
                "System Deployment",
                "Other");

            PrimaryFieldLabel.Text =
                "System / Task / Reference *";

            PrimaryFieldEntry.Placeholder =
                "Enter system, task or ticket reference";

            QuantityFieldLabel.Text =
                "Tasks / Tickets";

            QuantityFieldEntry.Placeholder =
                "Enter number of tasks or tickets";

            MonetaryValueLabel.Text =
                "Business Value / Cost Impact (R)";

            MonetaryValueEntry.Placeholder =
                "Enter value if applicable";

            SecondaryMetricLabel.Text =
                "Users / Systems Assisted";

            SecondaryMetricEntry.Placeholder =
                "Enter number assisted";
        }


        // =========================================================
        // FINANCE
        // =========================================================

        private void ConfigureFinance()
        {
            FormTitleLabel.Text =
                "Finance Daily Activity";

            FormDescriptionLabel.Text =
                "Record financial processing and reporting activity.";

            AddActivityItems(
                "Invoice Processing",
                "Payment Processing",
                "Expense Review",
                "Financial Reporting",
                "Budget Review",
                "Account Reconciliation",
                "Other");

            PrimaryFieldLabel.Text =
                "Transaction / Report Reference *";

            PrimaryFieldEntry.Placeholder =
                "Enter transaction or report reference";

            QuantityFieldLabel.Text =
                "Transactions / Documents";

            QuantityFieldEntry.Placeholder =
                "Enter quantity processed";

            MonetaryValueLabel.Text =
                "Financial Value (R)";

            MonetaryValueEntry.Placeholder =
                "Enter financial value";

            SecondaryMetricLabel.Text =
                "Accounts / Items Processed";

            SecondaryMetricEntry.Placeholder =
                "Enter number processed";
        }


        // =========================================================
        // HUMAN RESOURCES
        // =========================================================

        private void ConfigureHumanResources()
        {
            FormTitleLabel.Text =
                "Human Resources Daily Activity";

            FormDescriptionLabel.Text =
                "Record employee, recruitment and HR administration activity.";

            AddActivityItems(
                "Recruitment",
                "Employee Support",
                "Training",
                "Leave Administration",
                "Performance Administration",
                "HR Documentation",
                "Other");

            PrimaryFieldLabel.Text =
                "Employee / Activity Reference *";

            PrimaryFieldEntry.Placeholder =
                "Enter employee or activity reference";

            QuantityFieldLabel.Text =
                "Cases / Activities";

            QuantityFieldEntry.Placeholder =
                "Enter number of activities";

            MonetaryValueLabel.Text =
                "Cost / Business Value (R)";

            MonetaryValueEntry.Placeholder =
                "Enter value if applicable";

            SecondaryMetricLabel.Text =
                "Employees Assisted";

            SecondaryMetricEntry.Placeholder =
                "Enter number of employees";
        }


        // =========================================================
        // MARKETING
        // =========================================================

        private void ConfigureMarketing()
        {
            FormTitleLabel.Text =
                "Marketing Daily Activity";

            FormDescriptionLabel.Text =
                "Record campaigns, leads and marketing activity.";

            AddActivityItems(
                "Campaign Activity",
                "Lead Generation",
                "Social Media",
                "Content Creation",
                "Market Research",
                "Promotion",
                "Other");

            PrimaryFieldLabel.Text =
                "Campaign / Activity *";

            PrimaryFieldEntry.Placeholder =
                "Enter campaign or activity";

            QuantityFieldLabel.Text =
                "Activities / Leads";

            QuantityFieldEntry.Placeholder =
                "Enter number of activities or leads";

            MonetaryValueLabel.Text =
                "Campaign Value / Spend (R)";

            MonetaryValueEntry.Placeholder =
                "Enter campaign value or spend";

            SecondaryMetricLabel.Text =
                "People Reached / Leads";

            SecondaryMetricEntry.Placeholder =
                "Enter reach or lead count";
        }


        // =========================================================
        // OPERATIONS
        // =========================================================

        private void ConfigureOperations()
        {
            FormTitleLabel.Text =
                "Operations Daily Activity";

            FormDescriptionLabel.Text =
                "Record operational tasks, processing and service activity.";

            AddActivityItems(
                "Operational Task",
                "Order Processing",
                "Inventory Activity",
                "Quality Check",
                "Logistics Activity",
                "Process Improvement",
                "Other");

            PrimaryFieldLabel.Text =
                "Process / Activity Reference *";

            PrimaryFieldEntry.Placeholder =
                "Enter process or activity reference";

            QuantityFieldLabel.Text =
                "Units / Tasks";

            QuantityFieldEntry.Placeholder =
                "Enter units or tasks processed";

            MonetaryValueLabel.Text =
                "Operational Value / Cost (R)";

            MonetaryValueEntry.Placeholder =
                "Enter value if applicable";

            SecondaryMetricLabel.Text =
                "Orders / Processes Completed";

            SecondaryMetricEntry.Placeholder =
                "Enter number completed";
        }


        // =========================================================
        // CUSTOMER SERVICE
        // =========================================================

        private void ConfigureCustomerService()
        {
            FormTitleLabel.Text =
                "Customer Service Daily Activity";

            FormDescriptionLabel.Text =
                "Record customer support and service activity.";

            AddActivityItems(
                "Customer Enquiry",
                "Complaint Resolution",
                "Customer Follow-up",
                "Service Request",
                "Customer Support",
                "Escalation",
                "Other");

            PrimaryFieldLabel.Text =
                "Customer / Case Reference *";

            PrimaryFieldEntry.Placeholder =
                "Enter customer or case reference";

            QuantityFieldLabel.Text =
                "Cases / Requests";

            QuantityFieldEntry.Placeholder =
                "Enter number of cases";

            MonetaryValueLabel.Text =
                "Customer / Business Value (R)";

            MonetaryValueEntry.Placeholder =
                "Enter value if applicable";

            SecondaryMetricLabel.Text =
                "Customers Assisted";

            SecondaryMetricEntry.Placeholder =
                "Enter number of customers";
        }


        // =========================================================
        // PROCUREMENT
        // =========================================================

        private void ConfigureProcurement()
        {
            FormTitleLabel.Text =
                "Procurement Daily Activity";

            FormDescriptionLabel.Text =
                "Record supplier, purchasing and procurement activity.";

            AddActivityItems(
                "Purchase Order",
                "Supplier Quotation",
                "Supplier Follow-up",
                "Supplier Evaluation",
                "Stock Procurement",
                "Contract Activity",
                "Other");

            PrimaryFieldLabel.Text =
                "Supplier / Purchase Reference *";

            PrimaryFieldEntry.Placeholder =
                "Enter supplier or purchase reference";

            QuantityFieldLabel.Text =
                "Orders / Items";

            QuantityFieldEntry.Placeholder =
                "Enter number of orders or items";

            MonetaryValueLabel.Text =
                "Procurement Value (R)";

            MonetaryValueEntry.Placeholder =
                "Enter procurement value";

            SecondaryMetricLabel.Text =
                "Suppliers / Requests";

            SecondaryMetricEntry.Placeholder =
                "Enter number of suppliers or requests";
        }


        // =========================================================
        // MANAGEMENT
        // =========================================================

        private void ConfigureManagement()
        {
            FormTitleLabel.Text =
                "Management Daily Activity";

            FormDescriptionLabel.Text =
                "Record management, planning and decision-making activity.";

            AddActivityItems(
                "Management Meeting",
                "Performance Review",
                "Strategic Planning",
                "Decision / Approval",
                "Department Review",
                "Stakeholder Engagement",
                "Other");

            PrimaryFieldLabel.Text =
                "Meeting / Activity Reference *";

            PrimaryFieldEntry.Placeholder =
                "Enter meeting or activity reference";

            QuantityFieldLabel.Text =
                "Activities / Decisions";

            QuantityFieldEntry.Placeholder =
                "Enter number of activities";

            MonetaryValueLabel.Text =
                "Business Value / Budget Impact (R)";

            MonetaryValueEntry.Placeholder =
                "Enter value if applicable";

            SecondaryMetricLabel.Text =
                "Employees / Departments Involved";

            SecondaryMetricEntry.Placeholder =
                "Enter number involved";
        }


        // =========================================================
        // GENERAL
        // =========================================================

        private void ConfigureGeneral()
        {
            FormTitleLabel.Text =
                "Daily Work Activity";

            FormDescriptionLabel.Text =
                "Record your work activity.";

            AddActivityItems(
                "Work Task",
                "Meeting",
                "Administration",
                "Customer Activity",
                "Project Activity",
                "Other");

            PrimaryFieldLabel.Text =
                "Activity Reference *";

            PrimaryFieldEntry.Placeholder =
                "Enter activity reference";

            QuantityFieldLabel.Text =
                "Quantity / Tasks";

            QuantityFieldEntry.Placeholder =
                "Enter quantity";

            MonetaryValueLabel.Text =
                "Business Value (R)";

            MonetaryValueEntry.Placeholder =
                "Enter value if applicable";

            SecondaryMetricLabel.Text =
                "Secondary Metric";

            SecondaryMetricEntry.Placeholder =
                "Enter number if applicable";
        }


        // =========================================================
        // ACTIVITY ITEMS
        // =========================================================

        private void AddActivityItems(
            params string[] activities)
        {
            ActivityTypePicker.Items.Clear();

            foreach (string activity in activities)
            {
                ActivityTypePicker.Items.Add(
                    activity);
            }

            ActivityTypePicker.SelectedIndex =
                -1;
        }


        // =========================================================
        // PICKER EVENTS
        // =========================================================

        private void OnActivityTypeChanged(
            object sender,
            EventArgs e)
        {
            UpdateSelectionSummary();
        }


        private void OnStatusChanged(
            object sender,
            EventArgs e)
        {
            UpdateSelectionSummary();
        }


        private void OnImpactChanged(
            object sender,
            EventArgs e)
        {
            UpdateSelectionSummary();
        }


        // =========================================================
        // UPDATE SELECTED OPTIONS
        // =========================================================

        private void UpdateSelectionSummary()
        {
            if (ActivityTypePicker == null ||
                StatusPicker == null ||
                ImpactPicker == null ||
                SelectionSummaryBorder == null ||
                SelectedOptionsLabel == null)
            {
                return;
            }

            string activity =
                ActivityTypePicker
                    .SelectedItem?
                    .ToString()
                ?? string.Empty;

            string status =
                StatusPicker
                    .SelectedItem?
                    .ToString()
                ?? string.Empty;

            string impact =
                ImpactPicker
                    .SelectedItem?
                    .ToString()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(activity) &&
                string.IsNullOrWhiteSpace(status) &&
                string.IsNullOrWhiteSpace(impact))
            {
                SelectedOptionsLabel.Text =
                    string.Empty;

                SelectionSummaryBorder.IsVisible =
                    false;

                return;
            }

            string activityDisplay =
                string.IsNullOrWhiteSpace(activity)
                    ? "-"
                    : activity;

            string statusDisplay =
                string.IsNullOrWhiteSpace(status)
                    ? "-"
                    : status;

            string impactDisplay =
                string.IsNullOrWhiteSpace(impact)
                    ? "-"
                    : impact;

            SelectedOptionsLabel.Text =
                $"Activity: {activityDisplay}\n" +
                $"Status: {statusDisplay}\n" +
                $"Impact: {impactDisplay}";

            SelectionSummaryBorder.IsVisible =
                true;
        }


        // =========================================================
        // SUBMIT
        // =========================================================

        private async void OnSubmitClicked(
            object sender,
            EventArgs e)
        {
            HideValidation();


            // -----------------------------------------------------
            // AUTHENTICATION
            // -----------------------------------------------------

            bool authenticated =
                await _apiService
                    .RestoreAuthorizationTokenAsync();

            if (!authenticated)
            {
                ShowValidation(
                    "Your login session has expired. Please sign in again.");

                return;
            }


            // -----------------------------------------------------
            // RECORD DATE
            // -----------------------------------------------------

            DateTime selectedDate =
                EntryDatePicker.Date.Date;

            if (selectedDate >
                DateTime.Today)
            {
                ShowValidation(
                    "Record date cannot be in the future.");

                return;
            }


            // -----------------------------------------------------
            // ACTIVITY TYPE
            // -----------------------------------------------------

            string activityType =
                ActivityTypePicker
                    .SelectedItem?
                    .ToString()
                ?? string.Empty;


            // -----------------------------------------------------
            // REFERENCE
            // -----------------------------------------------------

            string reference =
                PrimaryFieldEntry.Text?.Trim()
                ?? string.Empty;


            if (string.IsNullOrWhiteSpace(
                    activityType))
            {
                ShowValidation(
                    "Select an activity type.");

                return;
            }


            if (string.IsNullOrWhiteSpace(
                    reference))
            {
                ShowValidation(
                    "Enter the required activity reference.");

                return;
            }


            // -----------------------------------------------------
            // QUANTITY
            // -----------------------------------------------------

            int quantity = 0;

            if (!string.IsNullOrWhiteSpace(
                    QuantityFieldEntry.Text))
            {
                if (!int.TryParse(
                        QuantityFieldEntry.Text.Trim(),
                        out quantity))
                {
                    ShowValidation(
                        "Quantity must be a valid whole number.");

                    return;
                }

                if (quantity < 0)
                {
                    ShowValidation(
                        "Quantity cannot be negative.");

                    return;
                }
            }


            // -----------------------------------------------------
            // BUSINESS VALUE
            // -----------------------------------------------------

            decimal businessValue = 0;

            if (!string.IsNullOrWhiteSpace(
                    MonetaryValueEntry.Text))
            {
                string valueText =
                    MonetaryValueEntry.Text
                        .Trim()
                        .Replace("R", "")
                        .Replace(" ", "");

                if (!decimal.TryParse(
                        valueText,
                        out businessValue))
                {
                    ShowValidation(
                        "Business value must be a valid number.");

                    return;
                }

                if (businessValue < 0)
                {
                    ShowValidation(
                        "Business value cannot be negative.");

                    return;
                }
            }


            // -----------------------------------------------------
            // SECONDARY METRIC
            // -----------------------------------------------------

            int secondaryMetric = 0;

            if (!string.IsNullOrWhiteSpace(
                    SecondaryMetricEntry.Text))
            {
                if (!int.TryParse(
                        SecondaryMetricEntry.Text.Trim(),
                        out secondaryMetric))
                {
                    ShowValidation(
                        "Secondary metric must be a valid whole number.");

                    return;
                }

                if (secondaryMetric < 0)
                {
                    ShowValidation(
                        "Secondary metric cannot be negative.");

                    return;
                }
            }


            // -----------------------------------------------------
            // STATUS
            // -----------------------------------------------------

            string status =
                StatusPicker
                    .SelectedItem?
                    .ToString()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    status))
            {
                ShowValidation(
                    "Select the work status.");

                return;
            }


            // -----------------------------------------------------
            // BUSINESS IMPACT
            // -----------------------------------------------------

            string businessImpact =
                ImpactPicker
                    .SelectedItem?
                    .ToString()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    businessImpact))
            {
                ShowValidation(
                    "Select the business impact.");

                return;
            }


            // -----------------------------------------------------
            // NOTES
            // -----------------------------------------------------

            string notes =
                NotesEditor.Text?.Trim()
                ?? string.Empty;


            // -----------------------------------------------------
            // CONFIRM
            // -----------------------------------------------------

            bool confirm =
                await DisplayAlert(
                    "Submit Daily Record",

                    $"Date: {selectedDate:dd MMMM yyyy}\n" +
                    $"Activity: {activityType}\n" +
                    $"Reference: {reference}\n" +
                    $"Status: {status}\n" +
                    $"Impact: {businessImpact}\n\n" +
                    "Submit this daily activity record?",

                    "Submit",
                    "Cancel");

            if (!confirm)
            {
                return;
            }


            // -----------------------------------------------------
            // CREATE API REQUEST
            // -----------------------------------------------------

            DailyRecordRequest request =
                new DailyRecordRequest
                {
                    RecordDate =
                        selectedDate,

                    ActivityType =
                        activityType,

                    Reference =
                        reference,

                    Quantity =
                        quantity,

                    BusinessValue =
                        businessValue,

                    SecondaryMetric =
                        secondaryMetric,

                    Status =
                        status,

                    BusinessImpact =
                        businessImpact,

                    Notes =
                        notes
                };


            // -----------------------------------------------------
            // SEND TO API
            // -----------------------------------------------------

            bool success =
                await _dailyRecordService
                    .SubmitDailyRecordAsync(
                        request);

            if (!success)
            {
                ShowValidation(
                    "The record could not be submitted. Make sure the API is running and your login session is still valid.");

                return;
            }


            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            await DisplayAlert(
                "Record Submitted",
                $"Your activity for {selectedDate:dd MMMM yyyy} was submitted successfully.",
                "OK");

            ClearForm();

            await Navigation.PopAsync();
        }


        // =========================================================
        // CLEAR BUTTON
        // =========================================================

        private async void OnClearClicked(
            object sender,
            EventArgs e)
        {
            bool clear =
                await DisplayAlert(
                    "Clear Form",
                    "Clear all information entered on this form?",
                    "Clear",
                    "Cancel");

            if (!clear)
            {
                return;
            }

            ClearForm();
        }


        // =========================================================
        // CLEAR FORM
        // =========================================================

        private void ClearForm()
        {
            ActivityTypePicker.SelectedIndex =
                -1;

            PrimaryFieldEntry.Text =
                string.Empty;

            QuantityFieldEntry.Text =
                string.Empty;

            MonetaryValueEntry.Text =
                string.Empty;

            SecondaryMetricEntry.Text =
                string.Empty;

            StatusPicker.SelectedIndex =
                -1;

            ImpactPicker.SelectedIndex =
                -1;

            NotesEditor.Text =
                string.Empty;

            HideValidation();

            UpdateSelectionSummary();

            // Do not reset EntryDatePicker here.
            // This allows several historical records for the
            // same selected month/date to be entered if needed.
        }


        // =========================================================
        // VALIDATION
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
        // BACK
        // =========================================================

        private async void OnBackClicked(
            object sender,
            EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}