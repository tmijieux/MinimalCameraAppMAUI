using Microsoft.Extensions.Logging;
using MinimalCameraApp.Controls;
using MinimalCameraApp.Platform.Renderers.Camera;

namespace MinimalCameraApp
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
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                        .ConfigureMauiHandlers(handlers =>
                         {
#if __IOS__
                             handlers.AddHandler(typeof(CameraView), typeof(CameraViewHandler));
#elif __ANDROID__
                             handlers.AddHandler(typeof(CameraView), typeof(CameraViewRenderer));
#endif

                         });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
