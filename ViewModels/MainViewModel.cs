using System.Collections.ObjectModel;
using MagazynApp.Model;
using MagazynApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MagazynApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IStorageService _storageService;

    [ObservableProperty]
    private string _scanInput = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć lub wyszukać";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBoxOpen))]
    private Box? _currentBox;

    public bool IsBoxOpen => CurrentBox != null;

    public ObservableCollection<Box.BoxItem> CurrentItems { get; } = new();
    
    // Kolekcja dla wyszukanych kartonów
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

    public async Task InitializeLocalDatabaseAsync()
    {
        try { var testProduct = await _storageService.GetProductByCodeAsync("meow"); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
    }

    public async Task ExecuteProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;

        string scannedCode = ScanInput.Trim();
        ScanInput = string.Empty;

        // 1. Sprawdź czy to produkt
        var product = await _storageService.GetProductByCodeAsync(scannedCode);
        if (product != null)
        {
            // Jeśli mamy otwarty karton - dodaj produkt
            if (CurrentBox != null)
            {
                var existingItem = CurrentItems.FirstOrDefault(i => i.ProductSku == product.CodeOrIdGraffiti);
                if (existingItem != null) existingItem.Quantity += 1;
                else
                {
                    var newItem = new Box.BoxItem { BoxCode = CurrentBox.BoxCode, ProductId = product.CodeOrIdGraffiti, ProductSku = product.CodeOrIdGraffiti, ProductName = product.Name, Quantity = 1 };
                    CurrentBox.Items.Add(newItem);
                    CurrentItems.Add(newItem);
                    UpdateListIndices();
                }
                SaveCurrentBoxInternal();
                StatusMessage = $"Dodano/Zwiększono: {product.Name}";
                return;
            }
            
            // Jeśli karton nie jest otwarty - szukaj zamkniętych kartonów z tym produktem
            FoundClosedBoxes.Clear();
            var boxes = await _storageService.GetClosedBoxesContainingProductAsync(scannedCode);
            foreach (var b in boxes) FoundClosedBoxes.Add(b);
            
            StatusMessage = FoundClosedBoxes.Any() ? $"Znaleziono {boxes.Count} zamkniętych kartonów." : "Najpierw zeskanuj kod kartonu!";
            return;
        }

        // 2. Sprawdź czy to karton
        var existingBox = await _storageService.GetBoxByCodeAsync(scannedCode);
        if (existingBox != null)
        {
            if (CurrentBox != null) await SaveAndCloseBoxAsync();
            
            CurrentBox = existingBox;
            CurrentBox.LoadAfterRead();
            CurrentBox.IsClosed = false; // Otwieramy go
            CurrentItems.Clear();
            foreach (var item in CurrentBox.Items) CurrentItems.Add(item);
            UpdateListIndices();
            FoundClosedBoxes.Clear(); // Czyścimy wyniki wyszukiwania
            StatusMessage = $"Otwarto karton: {scannedCode}.";
        }
        else if (CurrentBox == null)
        {
            CurrentBox = await _storageService.GetOrCreateBoxAsync(scannedCode);
            CurrentBox.IsClosed = false;
            StatusMessage = $"Utworzono nowy karton: {scannedCode}.";
        }
    }

    private void SaveCurrentBoxInternal()
    {
        if (CurrentBox != null)
        {
            CurrentBox.Items = CurrentItems.ToList();
            CurrentBox.PrepareForSave();
            _ = _storageService.SaveBoxAsync(CurrentBox);
        }
    }

    public async Task SaveAndCloseBoxAsync()
    {
        if (CurrentBox == null) return;
        
        CurrentBox.IsClosed = true; 
        SaveCurrentBoxInternal();
        
        StatusMessage = $"Karton {CurrentBox.BoxCode} zamknięty i zapisany.";
        CurrentBox = null;
        CurrentItems.Clear();
    }
}