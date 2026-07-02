using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MagazynApp.Model;
using MagazynApp.Services;

namespace MagazynApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly StorageService _storageService = new();

    private string _scanInput = string.Empty;
    private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć lub wyszukać";
    private Box? _currentBox;

    public string ScanInput
    {
        get => _scanInput;
        set { _scanInput = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public Box? CurrentBox
    {
        get => _currentBox;
        set 
        { 
            _currentBox = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBoxOpen));
        }
    }

    public bool IsBoxOpen => CurrentBox != null;

    // Lista pozycji wyświetlana w CollectionView
    public ObservableCollection<BoxItem> CurrentItems { get; } = new();

    // Komenda obsługująca skok z czytnika kodów
    public ICommand ProcessScanCommand { get; }

    public MainViewModel()
    {
        ProcessScanCommand = new Command(async () => await ExecuteProcessScanAsync());
    
        // WYMUSZENIE INTERFEJSU: Załaduj wbudowany plik CSV przy starcie aplikacji
        Task.Run(async () => await _storageService.ImportFromCsvAsync());
    }

    private async Task ExecuteProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;

        string scannedCode = ScanInput.Trim();
        ScanInput = string.Empty; // Natychmiastowe czyszczenie pod kolejny skan

        // Sytuacja A: Brak otwartego kartonu -> Wyszukaj lub stwórz karton
        if (CurrentBox == null)
        {
            CurrentBox = await _storageService.GetOrCreateBoxAsync(scannedCode);
            
            CurrentItems.Clear();
            foreach (var item in CurrentBox.Items)
            {
                CurrentItems.Add(item);
            }

            if (CurrentItems.Count > 0)
                StatusMessage = $" Znaleziono i wczytano karton: {scannedCode} ({CurrentItems.Count} pozycji).";
            else
                StatusMessage = $" Utworzono NOWY karton: {scannedCode}. Możesz skanować produkty.";

            return;
        }

        // Sytuacja B: Karton jest otwarty -> Skanowanie produktu (weryfikacja z bazą pobraną z CSV)
        var product = await _storageService.GetProductByCodeAsync(scannedCode);
        
        if (product != null)
        {
            var existingItem = CurrentItems.FirstOrDefault(i => i.ProductSku == product.CodeOrIdGraffiti);

            if (existingItem != null)
            {
                existingItem.Quantity += 1;
                StatusMessage = $"Zwiększono ilość: {product.Name} (Suma: {existingItem.Quantity})";
            }
            else
            {
                var newItem = new BoxItem
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
        }
        else
        {
            StatusMessage = $" Nieznany kod: '{scannedCode}'. Brak produktu w bazie danych!";
        }
    }

    public async Task SaveAndCloseBoxAsync()
    {
        if (CurrentBox == null) return;

        // KROK KLUCZOWY: Przepisujemy stan z kolekcji widoku przed zapisem
        CurrentBox.Items = CurrentItems.ToList();

        // Trwały zapis do SQLite
        await _storageService.SaveBoxAsync(CurrentBox);

        StatusMessage = $"💾 Karton {CurrentBox.BoxCode} wraz z zawartością ({CurrentItems.Count} szt) zapisany trwale.";
    
        // Reset okna pod kolejny skan paczki
        CurrentBox = null;
        CurrentItems.Clear();
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    // Wywoływane automatycznie przez MainPage przy starcie aplikacji
    public async Task InitializeLocalDatabaseAsync()
    {
        try
        {
            // Sprawdzamy, czy produkt "meow" (lub jakikolwiek inny) już istnieje, 
            // żeby nie katować bazy importem przy każdym wejściu na ekran
            var testProduct = await _storageService.GetProductByCodeAsync("meow");
        
            if (testProduct == null)
            {
                if (!IsBoxOpen) StatusMessage = "🔄 Inicjalizacja bazy danych produktów...";
            
                // Ładujemy plik produkty.csv prosto z folderu Resources/Raw/
                bool success = await _storageService.ImportFromCsvAsync(); 
            
                if (success && !IsBoxOpen)
                    StatusMessage = "✅ Wbudowana baza produktów załadowana pomyślnie.";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd inicjalizacji bazy: {ex.Message}");
        }
    }
}