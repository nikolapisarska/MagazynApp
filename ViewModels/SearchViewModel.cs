using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MagazynApp.Services;
using MagazynApp.Model;

namespace MagazynApp.ViewModels;

public class FocusScannerMessage { }

[QueryProperty(nameof(ReloadBoxCode), "ReloadBoxCode")]
public partial class SearchViewModel : ObservableObject
{
    private readonly IStorageService _storageService;
    private readonly NavigationState _navState;

    [ObservableProperty] private string _scanInput = string.Empty;
    [ObservableProperty] private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć";
    [ObservableProperty] private string? _reloadBoxCode;
    [ObservableProperty] private bool _isVerificationMode;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    private Box? _currentBox;

    public ObservableCollection<string> RecentScans { get; private set; } = new();
    public bool IsEditable => CurrentBox != null && CurrentBox.Status != "Wysłany";

    public SearchViewModel(IStorageService storageService, NavigationState navState)
    {
        _storageService = storageService;
        _navState = navState;
    }

    partial void OnReloadBoxCodeChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            RefreshCurrentBox(value);
            ReloadBoxCode = null;
        }
    }

    private async Task RefreshCurrentBox(string boxCode)
    {
        var updatedBox = await _storageService.GetBoxByCodeAsync(boxCode);
    
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (updatedBox != null)
            {
                CurrentBox = updatedBox;
                WeakReferenceMessenger.Default.Send(new FocusScannerMessage());
            }
        });
    }

    [RelayCommand]
    private async Task AddProductAsync()
    {
        if (CurrentBox == null) return;
        _navState.ShouldReturnToSearch = true;
        await Shell.Current.GoToAsync($"{nameof(MainPage)}?BoxCode={CurrentBox.BoxCode}");
    }

    private void IncrementProductQuantity(string sku)
    {
        if (CurrentBox == null) return;

        var item = CurrentBox.Items.FirstOrDefault(i => i.ProductSku == sku);
        if (item != null)
        {
            item.ConfirmedQuantity++;
            StatusMessage = $"Zwiększono {item.ProductName}: {item.ConfirmedQuantity}/{item.Quantity}";
        }
        else
        {
            StatusMessage = $"Błąd: Produkt {sku} nie istnieje w tym kartonie.";
        }
    }

    [RelayCommand]
    private async Task ProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;
        string codeToSearch = ScanInput.Trim();
        ScanInput = string.Empty;

        if (IsVerificationMode)
        {
            if (CurrentBox == null)
            {
                StatusMessage = "Najpierw zeskanuj karton, aby wejść w tryb weryfikacji.";
            }
            else
            {
                IncrementProductQuantity(codeToSearch);
                await _storageService.UpdateBox(CurrentBox);
            }
        }
        else
        {
            var box = await _storageService.GetBoxByCodeAsync(codeToSearch);
            if (box == null)
            {
                StatusMessage = $"Błąd: Karton {codeToSearch} nie istnieje.";
            }
            else
            {
                CurrentBox = box;
                StatusMessage = $"Otwarto karton: {codeToSearch}";

                if (RecentScans.Contains(codeToSearch)) RecentScans.Remove(codeToSearch);
                RecentScans.Insert(0, codeToSearch);
                while (RecentScans.Count > 5) RecentScans.RemoveAt(5);
            }
        }

        WeakReferenceMessenger.Default.Send(new FocusScannerMessage());
    }

    [RelayCommand]
    private async Task EditQuantity(Item item)
    {
        if (!IsEditable) return;
        string? result = await Shell.Current.DisplayPromptAsync("Edytuj", "Podaj nową ilość", initialValue: item.Quantity.ToString(), keyboard: Keyboard.Numeric);
        if (int.TryParse(result, out int newQty))
        {
            int oldQty = item.Quantity;
            item.Quantity = newQty;
            await _storageService.LogAudit(CurrentBox!.BoxCode, item.ProductSku, oldQty, newQty, "Manualna korekta");
            await _storageService.UpdateBox(CurrentBox);
            await RefreshCurrentBox(CurrentBox.BoxCode);
        }
    }

    // Nowa metoda obsługująca kliknięcie w wiersz
    [RelayCommand]
    private async Task OpenIssuePopup(Item item)
    {
        // Dodajemy "Edytuj ilość" do listy opcji
        string? action = await Shell.Current.DisplayActionSheetAsync(
            $"Produkt: {item.ProductName}", 
            "Anuluj", 
            null, 
            "Edytuj ilość", 
            "Zaginięcie", 
            "Uszkodzenie");

        if (action == "Edytuj ilość")
        {
            // Wywołujemy istniejącą logikę edycji
            await EditQuantity(item);
        }
        else if (action == "Zaginięcie" || action == "Uszkodzenie")
        {
            item.IsMissing = (action == "Zaginięcie");
            item.IsDamaged = (action == "Uszkodzenie");
            item.IsFlagged = true;
        
            item.Notes = await Shell.Current.DisplayPromptAsync("Notatka", "Podaj powód:");
        
            await _storageService.UpdateBox(CurrentBox!);
            await RefreshCurrentBox(CurrentBox!.BoxCode);
            StatusMessage = $"Zgłoszono {action} dla {item.ProductName}";
        }
    }

    [RelayCommand]
    private async Task StartVerification()
    {
        if (CurrentBox == null)
        {
            await Shell.Current.DisplayAlert("Błąd", "Brak otwartego kartonu do weryfikacji.", "OK");
            return;
        }
    
        await Shell.Current.DisplayAlert("Sukces", "Weryfikacja zakończona", "OK");
    }
}