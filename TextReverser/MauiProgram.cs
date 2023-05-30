using Microsoft.Extensions.Logging;
using TextReverser.ViewModel;
using CommunityToolkit.Maui;

namespace TextReverser;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
        builder.Logging.AddDebug();
#endif
        //builder.Services.AddSingleton<ReverserDataService>();


        builder.Services.AddSingleton<ReverserDataViewModel>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
	}
}
