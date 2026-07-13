using System.Collections.ObjectModel;
using System.Text.Json;
using MagazynApp.Model;
using MagazynApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MagazynApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IStorageService _storageService;

    [ObservableProperty] private string _scanInput = string.Empty;
    [ObservableProperty] private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć lub wyszukać";

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsBoxOpen))]
    private Box? _currentBox;

    public bool IsBoxOpen => CurrentBox != null;

    public ObservableCollection<Item> CurrentItems { get; } = new();
    public ObservableCollection<Box> FoundClosedBoxes { get; } = new();

    public MainViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    private void UpdateListIndices()
    {
        for (int i = 0; i < CurrentItems.Count; i++)
        {
            CurrentItems[i].Lp = i + 1;
            CurrentItems[i].IsEven = (i + 1) % 2 == 0;
        }
    }

    [RelayCommand]
    private async Task ProcessScanAsync() => await ExecuteProcessScanAsync();

    [RelayCommand]
    private async Task SaveAndCloseAsync() => await SaveAndCloseBoxAsync();

    [RelayCommand]
    private async Task RemoveItem(Item item)
    {
        CurrentItems.Remove(item);
        CurrentBox?.Items.Remove(item);
        UpdateListIndices();
        await SaveCurrentBoxInternal();
    }

    [ObservableProperty] private Product? _foundProduct;

    public async Task ExecuteProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;

        string scannedCode = ScanInput.Trim();
        ScanInput = string.Empty;

        // 1. Sprawdź, czy skanowany kod to produkt
        var product = await _storageService.GetProductByCodeAsync(scannedCode);
        if (product != null)
        {
            FoundProduct = product;
            if (CurrentBox != null)
            {
                var existingItem = CurrentItems.FirstOrDefault(i => i.ProductSku == product.CodeOrIdGraffiti);
                if (existingItem != null)
                {
                    existingItem.Quantity += 1;
                }
                else
                {
                    var newItem = new Item
                    {
                        ProductId = product.CodeOrIdGraffiti,
                        ProductSku = product.CodeOrIdGraffiti,
                        ProductName = product.Name,
                        Quantity = 1
                    };
                    CurrentItems.Add(newItem);
                    CurrentBox.Items.Add(newItem);
                    UpdateListIndices();
                }

                await SaveCurrentBoxInternal();
                StatusMessage = $"Dodano: {product.Name}";
            }
            else
            {
                FoundClosedBoxes.Clear();
                var boxes = await _storageService.GetClosedBoxesContainingProductAsync(scannedCode);
                foreach (var b in boxes) FoundClosedBoxes.Add(b);
                StatusMessage = $"Znaleziono: {product.Name}. Zeskanuj karton, aby dodać.";
            }

            return;
        }

        // 2. Jeśli to nie produkt, sprawdź czy to karton
        if (CurrentBox != null)
        {
            StatusMessage = "Najpierw zamknij otwarty karton!";
            return; // Przerywa działanie, nie otwiera nowego kartonu
        }

        var existingBox = await _storageService.GetBoxByCodeAsync(scannedCode);
        if (existingBox != null)
        {
            // Jeśli mamy otwarty karton, zamknij go przed otwarciem kolejnego
            if (CurrentBox != null) await SaveAndCloseBoxAsync();

            CurrentBox = existingBox;
            CurrentBox.LoadAfterRead();
            CurrentBox.IsClosed = false;
            CurrentItems.Clear();
            foreach (var item in CurrentBox.Items) CurrentItems.Add(item);
            UpdateListIndices();
            FoundClosedBoxes.Clear();
            StatusMessage = $"Otwarto karton: {scannedCode}.";
        }
        else
        {
            // To jest nowy karton - nie ma go w bazie
            if (CurrentBox != null) await SaveAndCloseBoxAsync();

            CurrentBox = await _storageService.GetOrCreateBoxAsync(scannedCode);
            CurrentBox.IsClosed = false;

            // ZAPIS DO BAZY: To sprawi, że karton fizycznie pojawi się w pliku .db3
            await _storageService.SaveBoxAsync(CurrentBox);

            CurrentItems.Clear();
            UpdateListIndices();
            StatusMessage = $"Utworzono nowy karton: {scannedCode}.";
        }
    }

    private async Task SaveCurrentBoxInternal()
    {
        if (CurrentBox != null)
        {
            CurrentBox.Items = CurrentItems.ToList();
            CurrentBox.PrepareForSave();
            await _storageService.SaveBoxAsync(CurrentBox);
        }
    }

    public async Task SaveAndCloseBoxAsync()
    {
        if (CurrentBox == null) return;

        CurrentBox.IsClosed = true;
        await SaveCurrentBoxInternal();

        StatusMessage = $"Karton {CurrentBox.BoxCode} zamknięty.";
        CurrentBox = null;
        CurrentItems.Clear();
    }

    public async Task InitializeLocalDatabaseAsync()
    {
        try
        {
            // Tutaj możesz dodać dowolną logikę inicjalizacji, jeśli jest potrzebna
            System.Diagnostics.Debug.WriteLine("Baza danych została zainicjalizowana.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportDataAsync()
    {
        try
        {
            // 1. Przygotuj dane
            var products = await _storageService.GetAllProductsAsync();
            var boxes = await _storageService.GetAllBoxesAsync();
            var data = new { Products = products, Boxes = boxes };
            string json = JsonSerializer.Serialize(data);
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(json);

            // 2. Wywołaj zapis w oknie systemowym
            // Używamy Dispatchera, aby okno otworzyło się płynnie w wątku UI
            await Application.Current.Dispatcher.DispatchAsync(async () =>
            {
                using var stream = new MemoryStream(fileBytes);
            
                // To wywołuje okno zapisu "Zapisz jako..."
                var result = await CommunityToolkit.Maui.Storage.FileSaver.Default.SaveAsync("magazyn_backup.json", stream);
            
                if (result.IsSuccessful)
                {
                    StatusMessage = "Zapisano w: " + result.FilePath;
                }
                else
                {
                    StatusMessage = "Zapis anulowany.";
                }
            });
        }
        catch (Exception ex)
        {
            StatusMessage = "Błąd: " + ex.Message;
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }

    [RelayCommand]
    private async Task ImportDataAsync()
    {System.Diagnostics.Debug.WriteLine("Przycisk EXPORT został kliknięty!");
        try
        {
            var result = await FilePicker.Default.PickAsync();
            if (result == null) return;

            string json = await File.ReadAllTextAsync(result.FullPath);

            // Klasa pomocnicza dopasowana do Twojego eksportu
            var data = JsonSerializer.Deserialize<BackupData>(json);

            if (data != null)
            {
                // Import Produktów
                foreach (var product in data.Products)
                {
                    await _storageService.SaveProductAsync(product);
                }

                // Import Kartonów
                foreach (var box in data.Boxes)
                {
                    await _storageService.SaveBoxAsync(box);
                }

                StatusMessage = $"Zaimportowano {data.Products.Count} produktów i {data.Boxes.Count} kartonów.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Błąd importu: " + ex.Message;
        }
    }

// Klasa pomocnicza (możesz dodać ją na końcu pliku MainViewModel.cs lub w osobnym pliku)
    public class BackupData
    {
        public List<Product> Products { get; set; } = new();
        public List<Box> Boxes { get; set; } = new();
    }
}
