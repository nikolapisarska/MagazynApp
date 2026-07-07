using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MagazynApp.Model;
using MagazynApp.Services;
using CommunityToolkit.Mvvm.ComponentModel; // Używamy tego
using CommunityToolkit.Mvvm.Input;          // Używamy tego

namespace MagazynApp.ViewModels;

// 1. Klasa musi być 'partial' i dziedziczyć po 'ObservableObject'
public partial class MainViewModel : ObservableObject 
{
    private readonly IStorageService _storageService;

    // 2. Automatyczne właściwości (zastępują ręczne get/set z OnPropertyChanged)
    [ObservableProperty]
    private string _scanInput = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć lub wyszukać";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBoxOpen))] // Automatycznie aktualizuje IsBoxOpen gdy CurrentBox się zmieni
    private Box? _currentBox;

    public bool IsBoxOpen => CurrentBox != null;
    public ObservableCollection<Box.BoxItem> CurrentItems { get; } = new();

    public MainViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    // 3. Komendy tworzymy przez atrybut [RelayCommand]
    [RelayCommand]
    private async Task ProcessScanAsync() => await ExecuteProcessScanAsync();

    [RelayCommand]
    private async Task SaveAndCloseAsync() => await SaveAndCloseBoxAsync();

    [RelayCommand]
    private void Reset() => ResetUI();

    [RelayCommand]
    private void RemoveItem(Box.BoxItem item)
    {
        if (item != null && CurrentItems.Contains(item))
        {
            CurrentItems.Remove(item);
            StatusMessage = $"Usunięto: {item.ProductName}";
        }
    }

    // Logika biznesowa 
    private void ResetUI()
    {
        CurrentBox = null;
        CurrentItems.Clear();
        StatusMessage = "Zapisano. Zeskanuj nowy kod kartonu.";
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
                CurrentBox.Items = CurrentItems.ToList();
                CurrentBox.PrepareForSave();
                await _storageService.SaveBoxAsync(CurrentBox);
                string oldBoxCode = CurrentBox.BoxCode;
                
                CurrentBox = existingBox;
                CurrentBox.LoadAfterRead();
                CurrentItems.Clear();
                foreach (var item in CurrentBox.Items) CurrentItems.Add(item);
                
                StatusMessage = $"Zapisano {oldBoxCode} i otwarto karton: {scannedCode}.";
            }
            else
            {
                CurrentBox = existingBox;
                CurrentBox.LoadAfterRead();
                CurrentItems.Clear();
                foreach (var item in CurrentBox.Items) CurrentItems.Add(item);
                StatusMessage = $"Otwarto karton: {scannedCode}.";
            }
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
    public async Task SaveAndCloseBoxAsync()
    {
        if (CurrentBox == null) return;

        CurrentBox.Items = CurrentItems.ToList();
        await _storageService.SaveBoxAsync(CurrentBox);
        StatusMessage = $"Karton {CurrentBox.BoxCode} zapisany.";
        ResetUI();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
}