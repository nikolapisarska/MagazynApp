using System.Collections.ObjectModel;
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
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsBoxOpen))]
    private Box? _currentBox;

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
            // 1. Pobierz wszystkie dane z bazy
            // Musisz dodać metodę GetAllBoxesAsync() do swojego IStorageService
            var allBoxes = await _storageService.GetAllBoxesAsync(); 
        
            string json = System.Text.Json.JsonSerializer.Serialize(allBoxes);
            string fileName = "MagazynEksport.json";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
        
            // 2. Zapisz do pliku tymczasowego
            await File.WriteAllTextAsync(filePath, json);
        
            // 3. Udostępnij plik użytkownikowi (systemowe okno zapisu)
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Eksport danych",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Błąd", $"Nie udało się wyeksportować: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ImportDataAsync()
    {
        // Tutaj dodaj logikę importu (np. FilePicker)
        await Shell.Current.DisplayAlert("Info", "Funkcja importu została wywołana", "OK");
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

        var product = await _storageService.GetProductByCodeAsync(scannedCode);
        if (product != null)
        {
            FoundProduct = product;
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

    private void UpdateListIndices()
    {
        for (int i = 0; i < CurrentItems.Count; i++) { CurrentItems[i].Lp = i + 1; CurrentItems[i].IsEven = (i + 1) % 2 == 0; }
    }

    private async Task SaveCurrentBoxInternal()
    {
        if (CurrentBox != null) { CurrentBox.Items = CurrentItems.ToList(); CurrentBox.PrepareForSave(); await _storageService.SaveBoxAsync(CurrentBox); }
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
            System.Diagnostics.Debug.WriteLine("Baza danych została zainicjalizowana.");
        }
        catch (Exception ex) 
        { 
            System.Diagnostics.Debug.WriteLine(ex.Message); 
        }
    }
    
}