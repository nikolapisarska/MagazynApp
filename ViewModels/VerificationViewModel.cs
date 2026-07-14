using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagazynApp.Model;
using MagazynApp.Services;

namespace MagazynApp.ViewModels;

public partial class VerificationViewModel : ObservableObject
{
    private readonly IStorageService _storageService;
    
    [ObservableProperty] private string _scanInput;
    [ObservableProperty] private string _statusMessage;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private string _progressText;

    public ObservableCollection<Item> CurrentItems { get; } = new();

    public VerificationViewModel(IStorageService storageService) => _storageService = storageService;

    private void UpdateProgress()
    {
        int finished = CurrentItems.Count(x => x.ConfirmedQuantity >= x.Quantity || x.IsMissing || x.IsDamaged);
        ProgressValue = CurrentItems.Count == 0 ? 0 : (double)finished / CurrentItems.Count;
        ProgressText = $"{finished} / {CurrentItems.Count} produktów przetworzonych";
    }

    [RelayCommand]
    public async Task ProcessScanAsync()
    {
        var item = CurrentItems.FirstOrDefault(x => x.ProductSku == ScanInput);
        if (item != null)
        {
            item.ConfirmedQuantity++;
            StatusMessage = $"Zeskanowano: {item.ProductName}";
            UpdateProgress();
        }
        ScanInput = string.Empty;
    }

    [RelayCommand]
    public void ReportMissing(Item item)
    {
        item.IsMissing = true;
        UpdateProgress();
    }

    [RelayCommand]
    public void RemoveDamaged(Item item)
    {
        item.IsDamaged = true;
        UpdateProgress();
    }

    [RelayCommand]
    public async Task CloseBoxAsync()
    {
        if (CurrentItems.Any(x => x.ConfirmedQuantity < x.Quantity && !x.IsMissing && !x.IsDamaged))
        {
            bool answer = await Shell.Current.DisplayAlert("Uwaga", "Paczka nie jest kompletna. Czy wysłać raport o brakach?", "Tak", "Nie");
            if (!answer) return;
        }
        await Shell.Current.DisplayAlert("Sukces", "Karton zamknięty i wysłany do serwera.", "OK");
    }
}