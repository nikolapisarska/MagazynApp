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

    // Właściwość z powiadomieniem dla UI - zmiana otwiera/zamyka widoczność elementów
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

    #region Komendy Eksportu i Importu

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

    #endregion

    #region Logika Skanowania

    [RelayCommand]
    private async Task ProcessScanAsync() => await ExecuteProcessScanAsync();

    public async Task ExecuteProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;

        string scannedCode = ScanInput.Trim();
        ScanInput = string.Empty;

        // 1. Sprawdzamy, czy zeskanowany kod to PRODUKT
        var product = await _storageService.GetProductByCodeAsync(scannedCode);
        if (product != null)
        {
            FoundProduct = product;
            if (CurrentBox != null)
            {
                // Jeśli mamy otwarty karton, dodajemy produkt do listy
                var existingItem = CurrentItems.FirstOrDefault(i => i.ProductSku == product.CodeOrIdGraffiti);
                if (existingItem != null) existingItem.Quantity += 1;
                else
                {
                    var newItem = new Item { ProductId = product.CodeOrIdGraffiti, ProductSku = product.CodeOrIdGraffiti, ProductName = product.Name, Quantity = 1 };
                    CurrentItems.Add(newItem);
                    CurrentBox.Items.Add(newItem);
                    UpdateListIndices();
                    OnPropertyChanged(nameof(CurrentItems));
                }
                await SaveCurrentBoxInternal();
                StatusMessage = $"Dodano: {product.Name}";
            }
            else
            {
                // Jeśli nie mamy otwartego kartonu, szukamy, w których kartonach jest ten produkt
                FoundClosedBoxes.Clear();
                var boxes = await _storageService.GetClosedBoxesContainingProductAsync(scannedCode);
                foreach (var b in boxes) FoundClosedBoxes.Add(b);
                StatusMessage = $"Znaleziono: {product.Name}. Zeskanuj karton, aby dodać.";
            }
            return;
        }

        // 2. Jeśli to nie produkt, sprawdzamy czy to KARTON
        if (CurrentBox != null) 
        { 
            StatusMessage = "Najpierw zamknij otwarty karton!"; 
            return; 
        }

        var existingBox = await _storageService.GetBoxByCodeAsync(scannedCode);
        if (existingBox != null)
        {
            // Otwieramy istniejący karton
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
            // Tworzymy nowy karton, jeśli nie istnieje
            CurrentBox = await _storageService.GetOrCreateBoxAsync(scannedCode);
            CurrentBox.IsClosed = false;
            await _storageService.SaveBoxAsync(CurrentBox);
            CurrentItems.Clear();
            UpdateListIndices();
            StatusMessage = $"Utworzono nowy karton: {scannedCode}.";
        }
    }

    #endregion

    #region Operacje na kartonie

    [RelayCommand]
    private async Task SaveAndCloseAsync() => await SaveAndCloseBoxAsync();

    public async Task SaveAndCloseBoxAsync()
    {
        if (CurrentBox == null) return;
        CurrentBox.IsClosed = true;
        await SaveCurrentBoxInternal();
        StatusMessage = $"Karton {CurrentBox.BoxCode} zamknięty.";
        CurrentBox = null; 
        CurrentItems.Clear();
    }

    [RelayCommand]
    private async Task RemoveItem(Item item)
    {
        CurrentItems.Remove(item);
        CurrentBox?.Items.Remove(item);
        UpdateListIndices();
        await SaveCurrentBoxInternal();
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

    private void UpdateListIndices()
    {
        for (int i = 0; i < CurrentItems.Count; i++) 
        { 
            CurrentItems[i].Lp = i + 1; 
            CurrentItems[i].IsEven = (i + 1) % 2 == 0; 
        }
    }

    #endregion

    public async Task InitializeLocalDatabaseAsync()
    {
        System.Diagnostics.Debug.WriteLine("Baza danych gotowa.");
    }
}