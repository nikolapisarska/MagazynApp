using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MagazynApp.Model;
using MagazynApp.Services;

namespace MagazynApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly StorageService _storageService = new();

    private string _scanInput = string.Empty;
    private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć lub wyszukać";
    private Box? _currentBox;

    public string ScanInput
    {
        get => _scanInput;
        set { _scanInput = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public Box? CurrentBox
    {
        get => _currentBox;
        set 
        { 
            _currentBox = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBoxOpen));
        }
    }

    public bool IsBoxOpen => CurrentBox != null;

    // Lista pozycji wyświetlana w CollectionView
    public ObservableCollection<BoxItem> CurrentItems { get; } = new();

    // Komendy dla UI
    public ICommand ProcessScanCommand { get; }
    public ICommand ImportCsvCommand { get; }

    public MainViewModel()
    {
        // Wiążemy metody asynchroniczne z komendami MAUI
        ProcessScanCommand = new Command(async () => await ExecuteProcessScanAsync());
        ImportCsvCommand = new Command(async () => await ChooseAndImportCsvAsync());
    }

    private async Task ExecuteProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;

        string scannedCode = ScanInput.Trim();
        ScanInput = string.Empty; // Natychmiastowe czyszczenie pod kolejny skan

        // Sytuacja A: Brak otwartego kartonu -> Wyszukaj lub stwórz karton
        if (CurrentBox == null)
        {
            // Skan kodu kartonu – sprawdzamy w SQLite przez StorageService
            CurrentBox = await _storageService.GetOrCreateBoxAsync(scannedCode);
            
            CurrentItems.Clear();
            foreach (var item in CurrentBox.Items)
            {
                CurrentItems.Add(item);
            }

            if (CurrentItems.Count > 0)
                StatusMessage = $"📦 Znaleziono i wczytano karton: {scannedCode} ({CurrentItems.Count} pozycji).";
            else
                StatusMessage = $"📦 Utworzono NOWY karton: {scannedCode}. Możesz skanować produkty.";

            return;
        }

        // Sytuacja B: Karton jest otwarty -> Skanowanie produktu (weryfikacja z bazą CSV)
        var product = await _storageService.GetProductByCodeAsync(scannedCode);
        
        if (product != null)
        {
            var existingItem = CurrentItems.FirstOrDefault(i => i.ProductSku == product.CodeOrIdGraffiti);

            if (existingItem != null)
            {
                existingItem.Quantity += 1;
                StatusMessage = $"Zwiększono ilość: {product.Name} (Suma: {existingItem.Quantity})";
            }
            else
            {
                var newItem = new BoxItem
                {
                    BoxCode = CurrentBox.BoxCode,
                    ProductId = product.CodeOrIdGraffiti,
                    ProductSku = product.CodeOrIdGraffiti,
                    ProductName = product.Name,
                    Quantity = 1
                };

                CurrentBox.Items.Add(newItem);
                CurrentItems.Add(newItem);
                StatusMessage = $"Dodano: {product.Name}";
            }
        }
        else
        {
            StatusMessage = $"⚠️ Nieznany kod: '{scannedCode}'. Brak produktu w bazie danych!";
        }
    }

    public async Task SaveAndCloseBoxAsync()
    {
        if (CurrentBox == null) return;

        // Przepisujemy aktualny stan listy do obiektu przed zapisem
        CurrentBox.Items = CurrentItems.ToList();

        // Trwały zapis do SQLite (waga, wymiary, zjsonowane przedmioty)
        await _storageService.SaveBoxAsync(CurrentBox);

        StatusMessage = $"💾 Karton {CurrentBox.BoxCode} zapisany pomyślnie w bazie lokalnej.";
        
        // Reset okna pod kolejny skan
        CurrentBox = null;
        CurrentItems.Clear();
    }

    private async Task ChooseAndImportCsvAsync()
    {
        try
        {
            var options = new PickOptions
            {
                PickerTitle = "Wybierz plik z kartoteką produktów (CSV)",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "text/comma-separated-values", "text/csv" } },
                    { DevicePlatform.MacCatalyst, new[] { "csv" } }
                })
            };

            var result = await FilePicker.Default.PickAsync(options);
            
            if (result != null)
            {
                StatusMessage = "Trwa importowanie danych z pliku CSV...";
                bool success = await _storageService.ImportFromCsvAsync(result.FullPath);

                if (success)
                    StatusMessage = "✅ Produkty z systemu Graffiti zaimportowane pomyślnie!";
                else
                    StatusMessage = "❌ Błąd importu. Sprawdź strukturę pliku CSV.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"⚠️ Błąd systemowy wyboru pliku: {ex.Message}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}