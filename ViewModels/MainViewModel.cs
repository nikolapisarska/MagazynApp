using System.Collections.ObjectModel;
using MagazynApp.Model;
using MagazynApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MagazynApp.ViewModels;

// Klasa ViewModel odpowiedzialna za logikę strony głównej (MVVM)
public partial class MainViewModel : ObservableObject 
{
    private readonly IStorageService _storageService;

    // Właściwość powiązana z polem tekstowym (SearchBar) w widoku
    [ObservableProperty]
    private string _scanInput = string.Empty;

    // Właściwość wyświetlająca komunikaty statusu (np. "Dodano produkt")
    [ObservableProperty]
    private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć lub wyszukać";

    // Przechowuje aktualnie otwarty karton
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBoxOpen))] // Automatycznie odświeża IsBoxOpen, gdy zmieni się CurrentBox
    private Box? _currentBox;

    // Zwraca informację, czy jakikolwiek karton jest obecnie otwarty
    public bool IsBoxOpen => CurrentBox != null;
    
    // Lista produktów znajdujących się w obecnie otwartym kartonie
    public ObservableCollection<Box.BoxItem> CurrentItems { get; } = new();

    public MainViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    // --- Komendy (Commands) wywoływane z przycisków w interfejsie ---
    
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

    // Resetuje interfejs do stanu początkowego (zamyka karton)
    private void ResetUI()
    {
        CurrentBox = null;
        CurrentItems.Clear();
        StatusMessage = "Zapisano. Zeskanuj nowy kod kartonu.";
    }

    // Główna logika przetwarzania zeskanowanego kodu
    public async Task ExecuteProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;

        string scannedCode = ScanInput.Trim();
        ScanInput = string.Empty; // Czyścimy pole wejściowe po przetworzeniu

        // 1. Sprawdzamy czy zeskanowany kod to produkt
        var product = await _storageService.GetProductByCodeAsync(scannedCode);
        if (product != null)
        {
            if (CurrentBox != null)
            {
                // Jeśli karton otwarty, dodaj produkt lub zwiększ ilość
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

        // 2. Jeśli to nie produkt, sprawdzamy czy to karton (istniejący)
        var existingBox = await _storageService.GetBoxByCodeAsync(scannedCode);
        if (existingBox != null)
        {
            if (CurrentBox != null)
            {
                // Zamykamy poprzedni i otwieramy nowy
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
                // Otwieramy nowy karton
                CurrentBox = existingBox;
                CurrentBox.LoadAfterRead();
                CurrentItems.Clear();
                foreach (var item in CurrentBox.Items) CurrentItems.Add(item);
                StatusMessage = $"Otwarto karton: {scannedCode}.";
            }
        }
        // 3. Jeśli kod nie pasuje do produktu ani kartonu, tworzymy nowy karton
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

    // Metoda wywoływana przy starcie aplikacji, sprawdzająca bazę
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

    // Ręczny zapis i zamknięcie kartonu
    public async Task SaveAndCloseBoxAsync()
    {
        if (CurrentBox == null) return;

        CurrentBox.Items = CurrentItems.ToList();
        await _storageService.SaveBoxAsync(CurrentBox);
        StatusMessage = $"Karton {CurrentBox.BoxCode} zapisany.";
        ResetUI();
    }
}