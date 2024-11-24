using Microsoft.Extensions.Logging;
using Roadside.Services;
using Syncfusion.Maui.Core.Hosting;
using CommunityToolkit.Maui;
using The49.Maui.BottomSheet;
using Mopups.Hosting;
using Plugin.LocalNotification;


namespace Roadside
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()

                .UseMauiCommunityToolkit()
                .UseMauiMaps()
                .ConfigureMopups()
                .UseLocalNotification()
                .UseBottomSheet()
                .ConfigureSyncfusionCore()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
            builder.Services.AddSingleton<MainPage>();
#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

    }
}
