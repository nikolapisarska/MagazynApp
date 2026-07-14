using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagazynApp.Model;
using MagazynApp.Services;

public partial class VerificationViewModel : ObservableObject
{
    private readonly IStorageService _storageService;

    [ObservableProperty] private string _scanInput;
    [ObservableProperty] private string _statusMessage;
    public ObservableCollection<Item> CurrentItems { get; } = new();

    public VerificationViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [RelayCommand]
    public async Task ProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;

        string input = ScanInput.Trim();
        ScanInput = string.Empty; // Czyścimy pole po każdym skanowaniu

        // 1. Sprawdź, czy skanujemy karton
        var box = await _storageService.GetBoxByCodeAsync(input);
        if (box != null)
        {
            box.LoadAfterRead(); // Pamiętaj o załadowaniu zawartości z JSONa!
            CurrentItems.Clear();
            foreach (var item in box.Items)
            {
                CurrentItems.Add(item);
            }

            StatusMessage = $"Załadowano karton: {box.BoxCode}";
            return;
        }

        // 2. Jeśli skanujemy produkt (gdy karton jest już załadowany)
        var product = await _storageService.GetProductByCodeAsync(input);
        if (product != null)
        {
            var item = CurrentItems.FirstOrDefault(x => x.ProductSku == product.CodeOrIdGraffiti);
            if (item != null)
            {
                StatusMessage = $"Znaleziono produkt: {product.Name}";
                // Opcjonalnie: możesz tu dodać logikę podświetlenia w UI
            }
            else
            {
                StatusMessage = $"Produktu {product.Name} nie ma w tym kartonie!";
            }
        }
        else
        {
            StatusMessage = "Nieznany kod.";
        }
    }
}