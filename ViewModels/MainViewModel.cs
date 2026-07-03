using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using MagazynApp.Model;
using MagazynApp.Services;

namespace MagazynApp.ViewModels;

// Klasa ViewModel odpowiedzialna za logikę głównego widoku aplikacji
public class MainViewModel : INotifyPropertyChanged
{
    // Serwis obsługujący operacje na plikach/bazie danych
    private readonly StorageService _storageService = new();

    // Pola prywatne przechowujące stan widoku
    private string _scanInput = string.Empty;
    private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć lub wyszukać";
    private Box? _currentBox;

    // Właściwość powiązana z polem tekstowym skanera (z obsługą aktualizacji UI)
    public string ScanInput
    {
        get => _scanInput;
        set { _scanInput = value; OnPropertyChanged(); }
    }

    // Komunikat wyświetlany użytkownikowi w interfejsie
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    // Przechowuje aktualnie edytowany karton
    public Box? CurrentBox
    {
        get => _currentBox;
        set 
        { 
            _currentBox = value; 
            OnPropertyChanged();
            // Powiadomienie UI, że stan "IsBoxOpen" uległ zmianie
            OnPropertyChanged(nameof(IsBoxOpen));
        }
    }

    // Właściwość informująca czy jakiś karton jest aktualnie otwarty
    public bool IsBoxOpen => CurrentBox != null;

    // Kolekcja przedmiotów w aktualnie otwartym kartonie (automatycznie odświeża widok)
    public ObservableCollection<BoxItem> CurrentItems { get; } = new();

    // Definicje komend dla przycisków
    public ICommand ProcessScanCommand { get; }
    public ICommand SaveAndCloseCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand IncrementQuantityCommand { get; }
    public ICommand DecrementQuantityCommand { get; }
    public ICommand RemoveItemCommand { get; }

    // Konstruktor klasy
    public MainViewModel()
    {
        // Przypisanie logiki do komend
        ProcessScanCommand = new Command(async () => await ExecuteProcessScanAsync());
        SaveAndCloseCommand = new Command(async () => await SaveAndCloseBoxAsync());
        ResetCommand = new Command(() => ResetUI());
        
        RemoveItemCommand = new Command<BoxItem>(RemoveItem);
        
        // Rozpoczęcie importu danych w tle bez blokowania UI
        Task.Run(async () => await _storageService.ImportFromCsvAsync());
    }

    // Usuwa wybrany produkt z listy
    private void RemoveItem(BoxItem item)
    {
        if (item != null && CurrentItems.Contains(item))
        {
            CurrentItems.Remove(item);
            StatusMessage = $"Usunięto: {item.ProductName}";
        }
    }

    // Resetuje widok do stanu początkowego
    private void ResetUI()
    {
        CurrentBox = null;
        CurrentItems.Clear();
        StatusMessage = "Zresetowano. Zeskanuj nowy kod kartonu.";
    }

    // Aktualizuje ilość konkretnego przedmiotu w kartonie
    private void UpdateQuantity(BoxItem item, int delta)
    {
        if (item == null) return;
        int newQuantity = (item.Quantity ?? 0) + delta;
        if (newQuantity < 1) newQuantity = 1; // Nie pozwól na ilość mniejszą niż 1
        item.Quantity = newQuantity;
        StatusMessage = $"Zmieniono ilość: {item.ProductName} na {item.Quantity}";
    }

    // Główna logika obsługi skanowania
    public async Task ExecuteProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;

        string scannedCode = ScanInput.Trim();
        ScanInput = string.Empty; // Czyści pole skanera po każdym skanie

        // 1. Sprawdź, czy zeskanowany kod to produkt
        var product = await _storageService.GetProductByCodeAsync(scannedCode);
        if (product != null)
        {
            if (CurrentBox != null)
            {
                // Sprawdź, czy produkt już jest w kartonie
                var existingItem = CurrentItems.FirstOrDefault(i => i.ProductSku == product.CodeOrIdGraffiti);
                if (existingItem != null)
                {
                    existingItem.Quantity += 1;
                    StatusMessage = $"Zwiększono ilość: {product.Name}";
                }
                else
                {
                    // Dodaj nowy przedmiot do kartonu
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
                // Zapisz zmiany w kartonie po dodaniu przedmiotu
                CurrentBox.PrepareForSave();
                await _storageService.SaveBoxAsync(CurrentBox);
                return; 
            }
            else
            {
                StatusMessage = "Najpierw zeskanuj kod istniejącego kartonu!";
                return;
            }
        }

        // 2. Jeśli nie produkt, sprawdź czy kod należy do kartonu
        var existingBox = await _storageService.GetBoxByCodeAsync(scannedCode);
    
        if (existingBox != null)
        {
            // Zapisz obecny karton, jeśli inny był otwarty
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
                // Otwórz nowy karton
                CurrentBox = existingBox;
                CurrentBox.LoadAfterRead();
                CurrentItems.Clear();
                foreach (var item in CurrentBox.Items) CurrentItems.Add(item);
                StatusMessage = $"Otwarto karton: {scannedCode}.";
            }
        }
        else if (CurrentBox == null) 
        {
            // C) Tworzenie nowego kartonu, jeśli kod nie istnieje
            CurrentBox = await _storageService.GetOrCreateBoxAsync(scannedCode);
            StatusMessage = $"Utworzono nowy karton: {scannedCode}.";
        }
        else 
        {
            // D) Błąd: Kod nie jest ani produktem, ani kartonem
            StatusMessage = $"Błąd: Nie rozpoznano kodu '{scannedCode}' (nie jest ani produktem, ani kartonem).";
        }
    }

    // Kończy edycję i zapisuje stan kartonu
    public async Task SaveAndCloseBoxAsync()
    {
        if (CurrentBox == null) return;

        CurrentBox.Items = CurrentItems.ToList();
        await _storageService.SaveBoxAsync(CurrentBox);
        StatusMessage = $" Karton {CurrentBox.BoxCode} zapisany.";
        ResetUI();
    }

    // Inicjalizacja bazy danych (sprawdzenie dostępności danych)
    public async Task InitializeLocalDatabaseAsync()
    {
        try
        {
            var testProduct = await _storageService.GetProductByCodeAsync("meow");
            if (testProduct == null)
            {
                // Import danych, jeśli baza jest pusta
                bool success = await _storageService.ImportFromCsvAsync(); 
                if (success) StatusMessage = "Baza produktów załadowana.";
            }
        }
        catch (Exception ex)
        {
            // Logowanie błędów do konsoli diagnostycznej
            System.Diagnostics.Debug.WriteLine($"Błąd inicjalizacji: {ex.Message}");
        }
    }

    // Implementacja interfejsu INotifyPropertyChanged dla powiązań danych
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
}