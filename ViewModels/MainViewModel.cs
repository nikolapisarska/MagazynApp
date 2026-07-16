using System.Collections.ObjectModel;
using System.Text.Json;
using MagazynApp.Model;
using MagazynApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MagazynApp.ViewModels;

[QueryProperty(nameof(BoxCodeToLoad), "BoxCode")]
public partial class MainViewModel : ObservableObject
{
    private readonly IStorageService _storageService;
    private readonly NavigationState _navState;

    [ObservableProperty] private string _scanInput = string.Empty;
    [ObservableProperty] private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć lub wyszukać";
    [ObservableProperty] private Product? _foundProduct;
    [ObservableProperty] private string? _boxCodeToLoad;

    private Box? _currentBox;
    public Box? CurrentBox 
    {
        get => _currentBox;
        set { if (SetProperty(ref _currentBox, value)) OnPropertyChanged(nameof(IsBoxOpen)); }
    }

    public bool IsBoxOpen => CurrentBox != null;
    public ObservableCollection<Item> CurrentItems { get; } = new();
    public ObservableCollection<Box> FoundClosedBoxes { get; } = new();

    public MainViewModel(IStorageService storageService, NavigationState navState)
    {
        _storageService = storageService;
        _navState = navState;
    }

    partial void OnBoxCodeToLoadChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            LoadBoxByCode(value);
            BoxCodeToLoad = null;
        }
    }

    [RelayCommand]
    private async Task ExportDataAsync()
    {
        try
        {
            string action = await Shell.Current.DisplayActionSheet("Co chcesz wyeksportować?", "Anuluj", null, "Produkty", "Kartony");
            if (action == "Anuluj") return;

            string json = action == "Produkty" ? JsonSerializer.Serialize(await _storageService.GetProductsAsync()) : JsonSerializer.Serialize(await _storageService.GetBoxesAsync());
            string fileName = $"{action}_{DateTime.Now:yyyyMMddHHmm}.json";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, json);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = $"Eksport: {action}", File = new ShareFile(filePath) });
        }
        catch (Exception ex) { await Shell.Current.DisplayAlert("Błąd", ex.Message, "OK"); }
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
            if (action == "Produkty") await _storageService.SaveProductsAsync(JsonSerializer.Deserialize<List<Product>>(jsonContent) ?? new());
            else await _storageService.SaveBoxesAsync(JsonSerializer.Deserialize<List<Box>>(jsonContent) ?? new());
            await Shell.Current.DisplayAlert("Sukces", "Dane zaimportowane.", "OK");
        }
        catch (Exception ex) { await Shell.Current.DisplayAlert("Błąd", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task ProcessScanAsync() => await ExecuteProcessScanAsync();

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

        var box = await _storageService.GetBoxByCodeAsync(scannedCode) ?? await _storageService.GetOrCreateBoxAsync(scannedCode);
        CurrentBox = box;
        CurrentBox.LoadAfterRead();
        CurrentBox.IsClosed = false;
        CurrentItems.Clear();
        foreach (var item in CurrentBox.Items) CurrentItems.Add(item);
        UpdateListIndices();
        StatusMessage = $"Otwarto karton: {scannedCode}.";
    }

    [RelayCommand]
    public async Task SaveAndCloseAsync()
    {
        if (CurrentBox == null) return;
        
        string codeToReturn = CurrentBox.BoxCode;
        CurrentBox.IsClosed = true;
        await SaveCurrentBoxInternal();
        
        StatusMessage = $"Karton {codeToReturn} zamknięty.";
        CurrentBox = null; 
        CurrentItems.Clear();
        
        // Powrót następuje tylko, jeśli weszliśmy przez Weryfikację
        if (_navState.ShouldReturnToSearch)
        {
            _navState.ShouldReturnToSearch = false; // Reset flagi
            await Shell.Current.GoToAsync($"BoxSearchPage?ReloadBoxCode={codeToReturn}");
        }
        else
        {
            StatusMessage = "Karton zamknięty. Możesz kontynuować skanowanie.";
        }
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

    private async void LoadBoxByCode(string boxCode)
    {
        var box = await _storageService.GetBoxByCodeAsync(boxCode);
        if (box != null)
        {
            CurrentBox = box;
            CurrentBox.LoadAfterRead();
            CurrentBox.IsClosed = false;
            CurrentItems.Clear();
            foreach (var item in CurrentBox.Items) CurrentItems.Add(item);
            UpdateListIndices();
            StatusMessage = $"Otwarto karton: {boxCode}";
        }
    }

    public async Task InitializeLocalDatabaseAsync() => await _storageService.InitializeAsync();
}