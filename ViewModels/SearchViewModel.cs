using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagazynApp.Services;
using MagazynApp.Model;

namespace MagazynApp.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly IStorageService _storageService;

    [ObservableProperty]
    private string _scannedCode = string.Empty;

    [ObservableProperty]
    private Box? _currentBox;

    [ObservableProperty]
    private bool _isVerificationMode;

    public SearchViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(ScannedCode)) return;

        // Upewnij się, że w IStorageService istnieje metoda GetBoxByCode
        var box = await _storageService.GetBoxByCode(ScannedCode);

        if (box == null)
        {
            bool createNew = await Shell.Current.DisplayAlert("Brak w bazie", 
                "Karton nie istnieje. Czy stworzyć nowy?", "Tak", "Nie");
            
            if (createNew) { /* Logika tworzenia nowego kartonu */ }
        }
        else
        {
            CurrentBox = box;
            await Shell.Current.GoToAsync("BoxDetailsPage");
        }
    }

    [RelayCommand]
    private void StartVerification() 
    {
        if (CurrentBox == null) return;

        IsVerificationMode = true;
        
        foreach (var item in CurrentBox.Items)
        {
            item.ConfirmedQuantity = 0;
            // StatusColor jest obliczany w modelu (Ignore), więc nie musimy go tu ustawiać ręcznie
        }
    }

    [RelayCommand]
    private async Task EditQuantity(Item item)
    {
        string? result = await Shell.Current.DisplayPromptAsync("Edytuj", "Podaj nową ilość", 
            initialValue: item.Quantity.ToString(), keyboard: Keyboard.Numeric);
        
        if (int.TryParse(result, out int newQty))
        {
            item.Quantity = newQty;
            // Wywołanie zapisu do bazy
            // await _storageService.Update(CurrentBox);
        }
    }

    public void ProcessScannedItem(string productCode)
    {
        if (!IsVerificationMode || CurrentBox == null) return;

        var item = CurrentBox.Items.FirstOrDefault(i => i.ProductSku == productCode);
        
        if (item != null)
        {
            item.ConfirmedQuantity++;
        }
        else
        {
            // Używamy DisplayAlertAsync (poprawka dla ostrzeżenia CS0618)
            MainThread.BeginInvokeOnMainThread(async () => 
            {
                await Shell.Current.DisplayAlert("Błąd", "Produkt nie należy do tego kartonu!", "OK");
            });
        }
    }
}