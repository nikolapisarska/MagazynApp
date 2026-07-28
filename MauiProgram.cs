using CommunityToolkit.Maui; 
using MagazynApp.Services;
using MagazynApp.ViewModels;
using Microsoft.Extensions.Logging;

namespace MagazynApp;

/// <summary>
/// Klasa konfiguracyjna aplikacji MAUI. Odpowiada za rejestrację czcionek, 
/// bibliotek zewnętrznych (Community Toolkit) oraz wstrzykiwanie zależności (DI) 
/// dla widoków (Views) i modeli widoków (ViewModels).
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() // Rejestracja pakietu rozszerzeń MAUI Community Toolkit
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
       
        // Rejestracja serwisu bazy danych jako Singleton (jedna instancja w całej aplikacji)
        builder.Services.AddSingleton<IStorageService, StorageService>();

        // Rejestracja stron oraz ich ViewModeli (wzorzec MVVM) jako obiekty Transient (tworzone na żądanie)
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MainViewModel>();
        
        builder.Services.AddTransient<BoxSearchPage>();
        builder.Services.AddTransient<SearchViewModel>();

        builder.Services.AddTransient<DashboardPage>();
        
#if DEBUG
        builder.Logging.AddDebug(); // Włączenie logowania debugowania w trybie testowym
#endif
        
        return builder.Build();
    }
}