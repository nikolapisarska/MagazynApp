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
    [ObservableProperty] private Product? _foundProduct;

    // Pełna właściwość z powiadomieniem dla UI (IsVisible)
    private Box? _currentBox;
    public Box? CurrentBox 
    {
        get => _currentBox;
        set 
        {
            if (SetProperty(ref _currentBox, value))
            {
                OnPropertyChanged(nameof(IsBoxOpen));
            }
        }
    }

    public bool IsBoxOpen => CurrentBox != null;

    public ObservableCollection<Item> CurrentItems { get; } = new();
    public ObservableCollection<Box> FoundClosedBoxes { get; } = new();

    public MainViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [RelayCommand]
    private async Task ExportDataAsync()
    {
        try
        {
            string action = await Shell.Current.DisplayActionSheet("Co chcesz wyeksportować?", "Anuluj", null, "Produkty", "Kartony");
            if (action == "Anuluj") return;

            string json = action == "Produkty" 
                ? JsonSerializer.Serialize(await _storageService.GetProductsAsync())
                : JsonSerializer.Serialize(await _storageService.GetBoxesAsync());

            string fileName = $"{action}_{DateTime.Now:yyyyMMddHHmm}.json";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, json);

            await Share.Default.RequestAsync(new ShareFileRequest { Title = $"Eksport: {action}", File = new ShareFile(filePath) });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Błąd", $"Nie udało się wyeksportować: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ImportDataAsync()
    {
        try
        {
            string action = await Shell.Current.DisplayActionSheet("Co importujesz?", "Anuluj", null, "Produkty", "Kartony");
            if (action == "Anuluj") return;

            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Wybierz plik JSON" });
            if (result == null) return;

            string jsonContent = await File.ReadAllTextAsync(result.FullPath);

            if (action == "Produkty")
                await _storageService.SaveProductsAsync(JsonSerializer.Deserialize<List<Product>>(jsonContent) ?? new());
            else
                await _storageService.SaveBoxesAsync(JsonSerializer.Deserialize<List<Box>>(jsonContent) ?? new());

            await Shell.Current.DisplayAlert("Sukces", "Dane zaimportowane.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Błąd", ex.Message, "OK");
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

    public async Task ExecuteProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;

        string scannedCode = ScanInput.Trim();
        ScanInput = string.Empty;

        var product = await _storageService.GetProductByCodeAsync(scannedCode);
        if (product != null)
        {
            if (CurrentBox != null)
            {
                var existingItem = CurrentItems.FirstOrDefault(i => i.ProductSku == product.CodeOrIdGraffiti);
                if (existingItem != null) existingItem.Quantity += 1;
                else
                {
                    var newItem = new Item { ProductId = product.CodeOrIdGraffiti, ProductSku = product.CodeOrIdGraffiti, ProductName = product.Name, Quantity = 1 };
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

        if (CurrentBox != null) { StatusMessage = "Najpierw zamknij otwarty karton!"; return; }

        var existingBox = await _storageService.GetBoxByCodeAsync(scannedCode);
        if (existingBox != null)
        {
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
            CurrentBox = await _storageService.GetOrCreateBoxAsync(scannedCode);
            CurrentBox.IsClosed = false;
            await _storageService.SaveBoxAsync(CurrentBox);
            CurrentItems.Clear();
            UpdateListIndices();
            StatusMessage = $"Utworzono nowy karton: {scannedCode}.";
        }
    }

    public async Task SaveAndCloseBoxAsync()
    {
        if (CurrentBox == null) return;
        CurrentBox.IsClosed = true;
        await SaveCurrentBoxInternal();
        StatusMessage = $"Karton {CurrentBox.BoxCode} zamknięty.";
        CurrentBox = null; // To automatycznie ukryje przycisk w UI
        CurrentItems.Clear();
    }

    private void UpdateListIndices()
    {
        for (int i = 0; i < CurrentItems.Count; i++) 
        { 
            CurrentItems[i].Lp = i + 1; 
            CurrentItems[i].IsEven = (i + 1) % 2 == 0; 
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

    public async Task InitializeLocalDatabaseAsync()
    {
        // Logika inicjalizacji (opcjonalnie wywołana w konstruktorze lub przy starcie)
        System.Diagnostics.Debug.WriteLine("Baza danych gotowa.");
    }
}