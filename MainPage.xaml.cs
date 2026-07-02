using MagazynApp.ViewModels;

namespace MagazynApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Zawsze wymuszaj fokus na skanerze po załadowaniu ekranu
        ScanEntry.Focus();

        // AUTOMATYCZNY IMPORT:
        // Sprawdzamy, czy baza danych została już napełniona. Jeśli nie,
        // wyciągamy wbudowany plik produkty.csv i ładujemy go automatycznie.
        try
        {
            var hasKey = Preferences.Default.Get("FirstTimeCsvImported", false);
            if (!hasKey)
            {
                // MAUI potrafi czytać pliki oznaczone jako 'MauiAsset' niezależnie od platformy
                using var stream = await FileSystem.OpenAppPackageFileAsync("produkty.csv");
                
                // Tworzymy tymczasową ścieżkę zapisu, żeby StorageService mógł go sparsować
                var tempPath = Path.Combine(FileSystem.CacheDirectory, "temp_produkty.csv");
                
                using (var fs = File.Create(tempPath))
                {
                    await stream.CopyToAsync(fs);
                }

                if (BindingContext is MainViewModel vm)
                {
                    vm.StatusMessage = "Inicjalizacja bazy towarowej z pliku CSV...";
                    // Korzystamy z metody w Twoim serwisie (musisz zmienić widoczność metody 
                    // importującej w StorageService z private/internal na public lub wywołać ją przez VM)
                    // Zakładając użycie komendy lub metody pomocniczej:
                    
                    // Dla uproszczenia – jeśli wkleiłeś komendę ImportCsv do VM, możemy wywołać bezpośrednio logikę serwisu:
                    var storageService = new Services.StorageService();
                    bool success = await storageService.ImportFromCsvAsync(tempPath);
                    
                    if (success)
                    {
                        Preferences.Default.Set("FirstTimeCsvImported", true);
                        vm.StatusMessage = "✅ Baza produktów Graffiti zainicjalizowana automatycznie!";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd auto-importu CSV: {ex.Message}");
        }
    }

    private async void OnSaveAndCloseClicked(object sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            await vm.SaveAndCloseBoxAsync();
            ScanEntry.Focus();
        }
    }
}