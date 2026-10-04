using InsightFlow.Services;
using Microsoft.Extensions.Logging;

namespace InsightFlow
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont(
                        "OpenSans-Regular.ttf",
                        "OpenSansRegular");

                    fonts.AddFont(
                        "OpenSans-Semibold.ttf",
                        "OpenSansSemibold");
                });

            // =====================================================
            // INSIGHTFLOW SERVICES
            // =====================================================

            builder.Services.AddSingleton<ApiService>();

            builder.Services.AddSingleton<AuthService>();

            builder.Services.AddSingleton<DailyRecordService>();


#if DEBUG
            builder.Logging.AddDebug();
#endif


            return builder.Build();
        }
    }
}