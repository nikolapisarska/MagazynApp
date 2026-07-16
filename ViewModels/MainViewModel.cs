using System.Collections.ObjectModel;
using System.Text.Json;
using MagazynApp.Model;
using MagazynApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MagazynApp.ViewModels;

[QueryProperty(nameof(BoxCodeToLoad), "BoxCode")]
[QueryProperty(nameof(IsEditingParam), "IsEditing")]
public partial class MainViewModel : ObservableObject
{
    private readonly IStorageService _storageService;
    private readonly NavigationState _navState;
    private bool _isCurrentlyEditing = false;

    [ObservableProperty] private string _scanInput = string.Empty;
    [ObservableProperty] private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć lub wyszukać";
    [ObservableProperty] private Product? _foundProduct;
    [ObservableProperty] private string? _boxCodeToLoad;
    [ObservableProperty] private string? _isEditingParam;

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

    public MainViewModel(IStorageService storageService, NavigationState navState)
    {
        _storageService = storageService;
        _navState = navState;
    }

    partial void OnIsEditingParamChanged(string? value)
    {
        _isCurrentlyEditing = (value == "true");
        IsEditingParam = null; // Resetujemy parametr po odczytaniu
    }

    partial void OnBoxCodeToLoadChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            LoadBoxByCode(value);
            BoxCodeToLoad = null; // Resetujemy parametr
        }
    }

    [RelayCommand]
    private async Task ProcessScanAsync() => await ExecuteProcessScanAsync();

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
                    OnPropertyChanged(nameof(CurrentItems));
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
            
            _navState.ShouldReturnToSearch = true;
            await Shell.Current.GoToAsync(nameof(MainPage)); 
            return; 
        }
        else
        {
            CurrentBox = await _storageService.GetOrCreateBoxAsync(scannedCode);
            CurrentBox.IsClosed = false;
            await _storageService.SaveBoxAsync(CurrentBox);
            CurrentItems.Clear();
            UpdateListIndices();
            StatusMessage = $"Utworzono nowy karton: {scannedCode}.";
            _navState.ShouldReturnToSearch = true;
        }
    }

    [RelayCommand]
    public async Task SaveAndCloseAsync()
    {
        if (CurrentBox == null) return;

        if (_isCurrentlyEditing)
        {
            await SaveCurrentBoxInternal();
            StatusMessage = $"Zapisano zmiany w: {CurrentBox.BoxCode}";
            CurrentBox = null;
            CurrentItems.Clear();
            _isCurrentlyEditing = false;
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            CurrentBox.IsClosed = true;
            await SaveCurrentBoxInternal();
            StatusMessage = $"Karton {CurrentBox.BoxCode} zamknięty.";
            CurrentBox = null; 
            CurrentItems.Clear();
        
            if (_navState.ShouldReturnToSearch)
            {
                _navState.ShouldReturnToSearch = false;
                await Shell.Current.GoToAsync(".."); 
            }
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
            foreach (var item in CurrentBox.Items) 
                CurrentItems.Add(item);
            
            UpdateListIndices();
            StatusMessage = $"Otwarto karton: {boxCode}";
        }
    }

    public async Task InitializeLocalDatabaseAsync() => await _storageService.InitializeAsync(); 
}