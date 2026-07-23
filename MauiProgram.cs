using CommunityToolkit.Maui; // 1. Dodaj ten namespace
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
            .UseMauiCommunityToolkit() // 2. DODAJ TO
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
       
        // 3. Uporządkowana rejestracja (usuń duplikaty)
        builder.Services.AddSingleton<IStorageService, StorageService>();
        builder.Services.AddSingleton<NavigationState>();

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MainViewModel>();
        
        builder.Services.AddTransient<BoxSearchPage>();
        builder.Services.AddTransient<SearchViewModel>();

        // W MauiProgram.cs
        builder.Services.AddTransient<DashboardPage>();
        
#if DEBUG
        builder.Logging.AddDebug();
#endif
        
        return builder.Build();
    }
}