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
    private readonly IStorageService _storageService;

    // Pola prywatne przechowujące stan widoku
    private string _scanInput = string.Empty;
    private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć lub wyszukać";
    private Box? _currentBox;

    // Właściwości powiązane z widokiem
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
    public ObservableCollection<BoxItem> CurrentItems { get; } = new();

    // Komendy
    public ICommand ProcessScanCommand { get; }
    public ICommand SaveAndCloseCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand RemoveItemCommand { get; }

    // Konstruktor przyjmujący interfejs serwisu (Wstrzykiwanie zależności)
    public MainViewModel(IStorageService storageService)
    {
        _storageService = storageService;

        // Inicjalizacja komend
        ProcessScanCommand = new Command(async () => await ExecuteProcessScanAsync());
        SaveAndCloseCommand = new Command(async () => await SaveAndCloseBoxAsync());
        ResetCommand = new Command(() => ResetUI());
        RemoveItemCommand = new Command<BoxItem>(RemoveItem);
        
        // Rozpoczęcie importu danych w tle
        Task.Run(async () => await _storageService.ImportFromCsvAsync());
    }

    private void RemoveItem(BoxItem item)
    {
        if (item != null && CurrentItems.Contains(item))
        {
            CurrentItems.Remove(item);
            StatusMessage = $"Usunięto: {item.ProductName}";
        }
    }

    private void ResetUI()
    {
        CurrentBox = null;
        CurrentItems.Clear();
        StatusMessage = "Zresetowano. Zeskanuj nowy kod kartonu.";
    }

    public async Task ExecuteProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;

        string scannedCode = ScanInput.Trim();

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
                CurrentBox.PrepareForSave();
                await _storageService.SaveBoxAsync(CurrentBox);
                return; 
            }
            StatusMessage = "Najpierw zeskanuj kod istniejącego kartonu!";
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
                bool success = await _storageService.ImportFromCsvAsync(); 
                if (success) StatusMessage = "Baza produktów załadowana.";
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