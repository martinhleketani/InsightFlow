using System.Net.Http.Headers;

namespace InsightFlow.Services
{
    public class ApiService
    {
        private const string ApiBaseUrl =
            "https://localhost:7212/";

        private const string TokenKey =
            "auth_token";

        private readonly HttpClient _httpClient;


        public ApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress =
                    new Uri(ApiBaseUrl),

                Timeout =
                    TimeSpan.FromSeconds(30)
            };
        }


        public HttpClient Client =>
            _httpClient;


        public void SetAuthorizationToken(
            string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ClearAuthorizationToken();

                return;
            }

            _httpClient
                .DefaultRequestHeaders
                .Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);
        }


        public async Task<bool>
            RestoreAuthorizationTokenAsync()
        {
            try
            {
                string? token =
                    await SecureStorage.Default
                        .GetAsync(TokenKey);

                if (string.IsNullOrWhiteSpace(token))
                {
                    ClearAuthorizationToken();

                    return false;
                }

                SetAuthorizationToken(token);

                return true;
            }
            catch
            {
                ClearAuthorizationToken();

                return false;
            }
        }


        public async Task<string?>
            GetAuthorizationTokenAsync()
        {
            try
            {
                return await SecureStorage.Default
                    .GetAsync(TokenKey);
            }
            catch
            {
                return null;
            }
        }


        public void ClearAuthorizationToken()
        {
            _httpClient
                .DefaultRequestHeaders
                .Authorization = null;
        }


        public void ClearSession()
        {
            ClearAuthorizationToken();

            SecureStorage.Default.Remove(
                "auth_token");

            SecureStorage.Default.Remove(
                "employee_account_id");

            SecureStorage.Default.Remove(
                "employee_id");

            SecureStorage.Default.Remove(
                "employee_first_name");

            SecureStorage.Default.Remove(
                "employee_last_name");

            SecureStorage.Default.Remove(
                "employee_email");

            SecureStorage.Default.Remove(
                "employee_department");

            SecureStorage.Default.Remove(
                "employee_department_id");

            SecureStorage.Default.Remove(
                "employee_department_code");

            SecureStorage.Default.Remove(
                "employee_role");
        }
    }
}