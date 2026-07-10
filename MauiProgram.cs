using MagazynApp.Services;
using MagazynApp.ViewModels;
using Microsoft.Extensions.Logging;

namespace MagazynApp;

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
			});
		
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddSingleton<IStorageService, StorageService>();
		builder.Services.AddTransient<MainViewModel>();
		#if DEBUG
		builder.Logging.AddDebug();
#endif
		return builder.Build();
	}
}