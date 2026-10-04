using System.Net.Http.Json;
using InsightFlow.Models;

namespace InsightFlow.Services
{
    public class DailyRecordService
    {
        private readonly ApiService _apiService;

        public DailyRecordService(ApiService apiService)
        {
            _apiService = apiService;
        }


        // =========================================================
        // SUBMIT DAILY RECORD
        // =========================================================

        public async Task<bool> SubmitDailyRecordAsync(
            DailyRecordRequest record)
        {
            try
            {
                // Restore the employee's JWT before
                // calling the protected API endpoint.
                bool authenticated =
                    await _apiService
                        .RestoreAuthorizationTokenAsync();

                if (!authenticated)
                {
                    return false;
                }


                HttpResponseMessage response =
                    await _apiService.Client.PostAsJsonAsync(
                        "api/DailyRecords",
                        record);


                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
