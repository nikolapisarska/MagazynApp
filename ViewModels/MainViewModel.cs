using System.Collections.ObjectModel;
using System.Text.Json;
using MagazynApp.Model;
using MagazynApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Encodings.Web;

namespace MagazynApp.ViewModels;

[QueryProperty(nameof(BoxCodeToLoad), "BoxCode")]
public partial class MainViewModel(IStorageService storageService) : ObservableObject
{
    private readonly IStorageService _storageService = storageService;

    [ObservableProperty] private string _scanInput = string.Empty;
    [ObservableProperty] private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć lub wyszukać";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsProductVisible))] private Product? _foundProduct;
    [ObservableProperty] private string? _boxCodeToLoad;

    public bool IsProductVisible => FoundProduct != null;

    private Box? _currentBox;
    public Box? CurrentBox 
    {
        get => _currentBox;
        set 
        { 
            if (SetProperty(ref _currentBox, value)) 
            { 
                OnPropertyChanged(nameof(IsBoxOpen)); 
                OnPropertyChanged(nameof(IsEditable)); 
            } 
        }
    }

    public bool IsBoxOpen => CurrentBox != null;
    
    public bool IsEditable => CurrentBox != null && 
                              CurrentBox.Status != BoxStatus.Sent && 
                              CurrentBox.Status != BoxStatus.Closed &&
                              !CurrentBox.IsClosed;

    public ObservableCollection<Item> CurrentItems { get; } = [];

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
            string action = await Shell.Current.DisplayActionSheetAsync("Co chcesz wyeksportować?", "Anuluj", null, "Produkty", "Kartony");
            if (action == "Anuluj") return;

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
            };

            string json = action == "Produkty" 
                ? JsonSerializer.Serialize(await _storageService.GetProductsAsync(), options) 
                : JsonSerializer.Serialize(await _storageService.GetBoxesAsync(), options); 
            
            string fileName = $"{action}_{DateTime.Now:yyyyMMddHHmm}.json";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            
            await File.WriteAllTextAsync(filePath, json);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = $"Eksport: {action}", File = new ShareFile(filePath) });
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Błąd", ex.Message, "OK"); }
    }

    [RelayCommand]
    private async Task ImportDataAsync()
    {
        try
        {
            string action = await Shell.Current.DisplayActionSheetAsync("Co importujesz?", "Anuluj", null, "Produkty", "Kartony");
            if (action == "Anuluj") return;
            
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Wybierz plik JSON" });
            if (result == null) return;
            
            string jsonContent = await File.ReadAllTextAsync(result.FullPath);
            if (action == "Produkty") 
                await _storageService.SaveProductsAsync(JsonSerializer.Deserialize<List<Product>>(jsonContent) ?? []);
            else 
                await _storageService.SaveBoxesAsync(JsonSerializer.Deserialize<List<Box>>(jsonContent) ?? []);
            
            await Shell.Current.DisplayAlertAsync("Sukces", "Dane zaimportowane.", "OK");
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Błąd", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task ProcessScanAsync()
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
                if (!IsEditable)
                {
                    StatusMessage = "Nie można edytować zamkniętego kartonu!";
                    return;
                }

                var existingItem = CurrentItems.FirstOrDefault(i => i.ProductSku == product.CodeOrIdGraffiti);
                if (existingItem != null) 
                {
                    existingItem.Quantity += 1;
                }
                else
                {
                    var newItem = new Item { ProductId = product.CodeOrIdGraffiti, ProductSku = product.CodeOrIdGraffiti, ProductName = product.Name, Quantity = 1 };
                    CurrentItems.Add(newItem);
                    CurrentBox.Items.Add(newItem);
                    UpdateListIndices();
                }

                if (CurrentBox.Status == BoxStatus.New)
                {
                    CurrentBox.Status = BoxStatus.InProgress;
                }

                await SaveCurrentBoxInternal();
                StatusMessage = $"Dodano: {product.Name}";
            }
            else
            {
                StatusMessage = $"Znaleziono: {product.Name}. Zeskanuj najpierw karton, aby dodać produkt.";
            }
            return;
        }

        var existingBox = await _storageService.GetBoxByCodeAsync(scannedCode);
        if (existingBox != null)
        {
            await SaveCurrentBoxInternal();
            SetCurrentBox(existingBox);
            StatusMessage = $"Przełączono do kartonu: {scannedCode}. Status: {CurrentBox?.Status}";
            return;
        }

        if (CurrentBox != null && IsEditable) 
        { 
            StatusMessage = "Nie znaleziono produktu ani takiego kartonu!"; 
            return; 
        }

        var box = await _storageService.GetOrCreateBoxAsync(scannedCode);
        SetCurrentBox(box);
        StatusMessage = $"Otwarto karton: {scannedCode}. Status: {CurrentBox?.Status}";
    }

    [RelayCommand]
    public async Task SaveAndReturnAsync()
    {
        if (CurrentBox == null) return;
        
        string codeToReturn = CurrentBox.BoxCode;
        await SaveCurrentBoxInternal();

        CurrentBox = null; 
        CurrentItems.Clear();
        FoundProduct = null; 
        StatusMessage = $"Zapisano karton {codeToReturn}. Możesz kontynuować skanowanie.";
    }

    [RelayCommand]
    private async Task RemoveItem(Item item)
    {
        if (!IsEditable) return; 

        CurrentItems.Remove(item);
        CurrentBox?.Items.Remove(item);
        UpdateListIndices();
        await SaveCurrentBoxInternal();
    }

    private async Task SaveCurrentBoxInternal()
    {
        if (CurrentBox != null) 
        { 
            CurrentBox.Items = [.. CurrentItems]; 
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
            SetCurrentBox(box);
            StatusMessage = $"Otwarto karton: {boxCode}";
        }
    }

    private void SetCurrentBox(Box box)
    {
        CurrentBox = box;
        CurrentBox.LoadAfterRead();
        ReloadItems(CurrentBox.Items);
        FoundProduct = null;
    }
    
    [RelayCommand]
    private async Task ImportItemsToBoxAsync()
    {
        if (CurrentBox == null || !IsEditable)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Najpierw otwórz edytowalny karton!", "OK");
            return;
        }

        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Wybierz plik z listą produktów (JSON)" });
            if (result == null) return;

            string jsonContent = await File.ReadAllTextAsync(result.FullPath);
            var importedItems = JsonSerializer.Deserialize<List<Item>>(jsonContent);

            if (importedItems != null)
            {
                foreach (var importedItem in importedItems)
                {
                    var existingItem = CurrentItems.FirstOrDefault(i => i.ProductSku == importedItem.ProductSku);
                    if (existingItem != null)
                        existingItem.Quantity += importedItem.Quantity;
                    else
                        CurrentItems.Add(importedItem);
                }
            
                UpdateListIndices();
                await SaveCurrentBoxInternal();
                await Shell.Current.DisplayAlertAsync("Sukces", "Produkty zostały zaimportowane.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Błąd importu", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await SaveCurrentBoxInternal();
        await Shell.Current.GoToAsync("///DashboardPage");
    }

    private void ReloadItems(IEnumerable<Item> newItems)
    {
        CurrentItems.Clear();
        foreach (var item in newItems) CurrentItems.Add(item);
        UpdateListIndices();
    }

    [RelayCommand]
    private async Task GoToVerificationAsync()
    {
        if (CurrentBox == null)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Brak aktywnego kartonu do weryfikacji.", "OK");
            return;
        }

        await SaveCurrentBoxInternal();
        await Shell.Current.GoToAsync($"BoxSearchPage?ReloadBoxCode={CurrentBox.BoxCode}");
    }
}