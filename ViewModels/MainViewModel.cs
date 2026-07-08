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

    public MainViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    //Logika pomocnicza 

    private void UpdateListIndices()
    {
        for (int i = 0; i < CurrentItems.Count; i++)
        {
            CurrentItems[i].Lp = i + 1;
            // Parzyste/nieparzyste dla kolorowania wierszy
            CurrentItems[i].IsEven = (i + 1) % 2 == 0;
        }
    }

    //Komendy 

    [RelayCommand]
    private async Task ProcessScanAsync() => await ExecuteProcessScanAsync();

    [RelayCommand]
    private async Task SaveAndCloseAsync() => await SaveAndCloseBoxAsync();

// Inicjalizacja 

    public async Task InitializeLocalDatabaseAsync()
    {
        try
        {
            var testProduct = await _storageService.GetProductByCodeAsync("meow");
            if (testProduct == null)
            {
                StatusMessage = "Baza produktów gotowa do pracy.";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd inicjalizacji: {ex.Message}");
        }
    }

    // Logika Główna 

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
                if (existingItem != null)
                {
                    existingItem.Quantity += 1;
                    StatusMessage = $"Zwiększono ilość: {product.Name}";
                }
                else
                {
                    var newItem = new Box.BoxItem
                    {
                        BoxCode = CurrentBox.BoxCode,
                        ProductId = product.CodeOrIdGraffiti,
                        ProductSku = product.CodeOrIdGraffiti,
                        ProductName = product.Name,
                        Quantity = 1
                    };
                    CurrentBox.Items.Add(newItem);
                    CurrentItems.Add(newItem);
                    UpdateListIndices(); // Odśwież numery po dodaniu
                    StatusMessage = $"Dodano: {product.Name}";
                }
                CurrentBox.PrepareForSave();
                await _storageService.SaveBoxAsync(CurrentBox);
                return;
            }
            StatusMessage = "Najpierw zeskanuj kod kartonu!";
            return;
        }

        var existingBox = await _storageService.GetBoxByCodeAsync(scannedCode);
        if (existingBox != null)
        {
            if (CurrentBox != null)
            {
                await SaveAndCloseBoxAsync(); 
            }
            CurrentBox = existingBox;
            CurrentBox.LoadAfterRead();
            CurrentItems.Clear();
            foreach (var item in CurrentBox.Items) CurrentItems.Add(item);
            UpdateListIndices();
            StatusMessage = $"Otwarto karton: {scannedCode}.";
        }
        else if (CurrentBox == null)
        {
            CurrentBox = await _storageService.GetOrCreateBoxAsync(scannedCode);
            StatusMessage = $"Utworzono nowy karton: {scannedCode}.";
        }
        else
        {
            StatusMessage = $"Błąd: Nie rozpoznano kodu '{scannedCode}'.";
        }
    }
    

    [RelayCommand]
    private void IncreaseQuantity(Box.BoxItem item)
    {
        item.Quantity++;
        SaveCurrentBoxInternal();
    }

    [RelayCommand]
    private void DecreaseQuantity(Box.BoxItem item)
    {
        if (item.Quantity > 1)
        {
            item.Quantity--;
            SaveCurrentBoxInternal();
        }
    }

  [RelayCommand]
private void RemoveItem(Box.BoxItem item)
{
    if (item != null && CurrentItems.Contains(item))
    {
        CurrentItems.Remove(item);
        UpdateListIndices();
        SaveCurrentBoxInternal(); // Metoda, która zapisuje obecny stan do pliku
        StatusMessage = $"Usunięto: {item.ProductName}";
    }
}

// Metoda pomocnicza zapewniająca synchronizację danych
    private void SaveCurrentBoxInternal()
    {
        if (CurrentBox != null)
        {
            CurrentBox.Items = CurrentItems.ToList();
            CurrentBox.PrepareForSave(); // synchronizacja JSONa
            _ = _storageService.SaveBoxAsync(CurrentBox);
        }
    }
    public async Task SaveAndCloseBoxAsync()
    {
        if (CurrentBox == null) return;
    
        SaveCurrentBoxInternal(); // Zapis ostateczny
        StatusMessage = $"Karton {CurrentBox.BoxCode} zapisany.";
    
        CurrentBox = null;
        CurrentItems.Clear();
    }
}