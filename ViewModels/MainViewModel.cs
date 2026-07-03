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

    public ObservableCollection<BoxItem> CurrentItems { get; } = new();

    // Komendy
    public ICommand ProcessScanCommand { get; }
    public ICommand SaveAndCloseCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand IncrementQuantityCommand { get; }
    public ICommand DecrementQuantityCommand { get; }

    public MainViewModel()
    {
        ProcessScanCommand = new Command(async () => await ExecuteProcessScanAsync());
        SaveAndCloseCommand = new Command(async () => await SaveAndCloseBoxAsync());
        ResetCommand = new Command(() => ResetUI());
        
        IncrementQuantityCommand = new Command<BoxItem>(item => UpdateQuantity(item, 1));
        DecrementQuantityCommand = new Command<BoxItem>(item => UpdateQuantity(item, -1));
        
        Task.Run(async () => await _storageService.ImportFromCsvAsync());
    }

    private void ResetUI()
    {
        CurrentBox = null;
        CurrentItems.Clear();
        StatusMessage = "Zresetowano. Zeskanuj nowy kod kartonu.";
    }

    private void UpdateQuantity(BoxItem item, int delta)
    {
        if (item == null) return;
        int newQuantity = (item.Quantity ?? 0) + delta;
        if (newQuantity < 1) newQuantity = 1;
        item.Quantity = newQuantity;
        StatusMessage = $"Zmieniono ilość: {item.ProductName} na {item.Quantity}";
    }

    private async Task ExecuteProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;

        string scannedCode = ScanInput.Trim();
        ScanInput = string.Empty; 

        if (CurrentBox == null)
        {
            CurrentBox = await _storageService.GetOrCreateBoxAsync(scannedCode);
            CurrentItems.Clear();
            foreach (var item in CurrentBox.Items)
            {
                CurrentItems.Add(item);
            }

            if (CurrentItems.Count > 0)
                StatusMessage = $"Znaleziono karton: {scannedCode} ({CurrentItems.Count} pozycji).";
            else
                StatusMessage = $"Utworzono NOWY karton: {scannedCode}.";
            return;
        }

        var product = await _storageService.GetProductByCodeAsync(scannedCode);
        if (product != null)
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
        }
        else
        {
            StatusMessage = $"Błąd: Nieznany kod '{scannedCode}'!";
        }
    }

    public async Task SaveAndCloseBoxAsync()
    {
        if (CurrentBox == null) return;

        CurrentBox.Items = CurrentItems.ToList();
        await _storageService.SaveBoxAsync(CurrentBox);
        StatusMessage = $"💾 Karton {CurrentBox.BoxCode} zapisany.";
        ResetUI();
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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}