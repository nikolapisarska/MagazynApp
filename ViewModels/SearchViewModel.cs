using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MagazynApp.Services;
using MagazynApp.Model;
using CommunityToolkit.Maui.Views; 
using MagazynApp.Views;


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
    [NotifyPropertyChangedFor(nameof(HasBoxLoaded))] 
    [NotifyPropertyChangedFor(nameof(CanCloseBox))]
    private Box? _currentBox;

    public bool HasBoxLoaded => CurrentBox != null;
    public ObservableCollection<string> RecentScans { get; private set; } = new();
    
    public bool IsEditable => CurrentBox != null && 
                              CurrentBox.Status != "Wysłany" && 
                              CurrentBox.Status != "Zamknięty" &&
                              CurrentBox.Status != "Closed" &&
                              !CurrentBox.IsClosed;

    public SearchViewModel(IStorageService storageService, NavigationState navState)
    {
        _storageService = storageService;
        _navState = navState;
    }

    public bool CanCloseBox => CurrentBox != null &&
                               CurrentBox.Status != "Wysłany" &&
                               CurrentBox.Status != "Zamknięty" &&
                               CurrentBox.Items.Any() &&
                               CurrentBox.Items.All(i => 
                                   (i.ConfirmedQuantity == i.Quantity || i.ConfirmedQuantity == 0) && 
                                   i.Quantity > 0 &&
                                   i.MissingQty == 0 && 
                                   i.DamagedQty == 0);

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
                SubscribeToItemsChanges(); 
                NotifyStateChanged();
                WeakReferenceMessenger.Default.Send(new FocusScannerMessage());
            }
        });
    }

    private void SubscribeToItemsChanges()
    {
        if (CurrentBox?.Items == null) return;

        foreach (var item in CurrentBox.Items)
        {
            item.PropertyChanged -= Item_PropertyChanged;
            item.PropertyChanged += Item_PropertyChanged;
        }
    }

    private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CanCloseBox));
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(CanCloseBox));
        OnPropertyChanged(nameof(IsEditable));
    }

    [RelayCommand]
    private async Task AddProductAsync()
    {
        if (CurrentBox == null || !IsEditable) return;
    
           _navState.ShouldReturnToSearch = true; 
    
      
        await Shell.Current.GoToAsync($"{nameof(MainPage)}?BoxCode={CurrentBox.BoxCode}");
    }

    private void IncrementProductQuantity(string sku)
    {
        if (CurrentBox == null || !IsEditable) return;

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
                await RefreshCurrentBox(CurrentBox.BoxCode);
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
                SubscribeToItemsChanges(); 
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

   [RelayCommand]
   private async Task OpenIssuePopup(Item item)
   {
        if (!IsEditable) return;

        string? action = await Shell.Current.DisplayActionSheetAsync(
            $"Produkt: {item.ProductName}", "Anuluj", null, 
            "Edytuj ilość", "Zgłoś braki", "Zgłoś uszkodzenie", "Dodaj/Edytuj notatkę", "Wyczyść zgłoszenia");

        if (action == "Dodaj/Edytuj notatkę")
        {
            string? note = await Shell.Current.DisplayPromptAsync("Notatka", "Wpisz uwagi:", initialValue: item.Notes);
            if (note != null)
            {
                item.Notes = note;
                item.IsFlagged = !string.IsNullOrEmpty(item.Notes) || item.MissingQty > 0 || item.DamagedQty > 0;
                await _storageService.UpdateBox(CurrentBox!);
                
                OnPropertyChanged(nameof(CanCloseBox));
                await RefreshCurrentBox(CurrentBox!.BoxCode);
            }
        }
        else if (action == "Edytuj ilość")
        {
            string? result = await Shell.Current.DisplayPromptAsync("Edytuj", "Podaj nową ilość (0, aby usunąć)", initialValue: item.Quantity.ToString(), keyboard: Keyboard.Numeric);
            if (int.TryParse(result, out int newQty))
            {
                // Zabezpieczenie przed liczbami ujemnymi
                if (newQty < 0) newQty = 0;

                int oldQty = item.Quantity;

                if (newQty == 0)
                {
                    bool confirm = await Shell.Current.DisplayAlert("Usuwanie", $"Czy na pewno chcesz usunąć produkt {item.ProductName} z kartonu?", "Tak", "Anuluj");
                    if (!confirm) return;

                    CurrentBox!.Items.Remove(item);
                    await _storageService.LogAudit(CurrentBox.BoxCode, item.ProductSku, oldQty, 0, "Usunięcie produktu (ilość 0)");
                }
                else
                {
                    item.Quantity = newQty;
                    await _storageService.LogAudit(CurrentBox!.BoxCode, item.ProductSku, oldQty, newQty, "Manualna korekta ilości");
                }

                await _storageService.UpdateBox(CurrentBox!);
        
                OnPropertyChanged(nameof(CanCloseBox));
                await RefreshCurrentBox(CurrentBox!.BoxCode);
            }
        }
        else if (action == "Zgłoś braki" || action == "Zgłoś uszkodzenie")
        {
            string? result = await Shell.Current.DisplayPromptAsync(action, "Podaj ilość:", keyboard: Keyboard.Numeric);
            if (int.TryParse(result, out int qty) && qty > 0) // Warunek qty > 0 blokuje zera i liczby ujemne
            {
                int dostepne = item.Quantity - item.MissingQty - item.DamagedQty;
                if (qty > dostepne) { await Shell.Current.DisplayAlert("Błąd", "Za duża ilość", "OK"); return; }
        
                if (action == "Zgłoś braki") item.MissingQty += qty;
                else item.DamagedQty += qty;

                item.IsFlagged = true;
                await _storageService.UpdateBox(CurrentBox!);
        
                OnPropertyChanged(nameof(CanCloseBox));
                await RefreshCurrentBox(CurrentBox!.BoxCode);
            }
        }
        else if (action == "Wyczyść zgłoszenia")
        {
            item.MissingQty = 0;
            item.DamagedQty = 0;
            item.Notes = string.Empty;
            item.IsFlagged = false;

            await _storageService.UpdateBox(CurrentBox!);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(CanCloseBox));
            });

            await RefreshCurrentBox(CurrentBox!.BoxCode);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(CanCloseBox));
            });
        }
    }

   [RelayCommand]
   private async Task StartVerification()
   {
       if (CurrentBox == null)
       {
           await Shell.Current.DisplayAlert("Błąd", "Brak otwartego kartonu.", "OK");
           return;
       }

       var popup = new VerificationSummaryPopup(CurrentBox, this);
       await Shell.Current.CurrentPage.ShowPopupAsync(popup);
   }

    [RelayCommand]
    private async Task CloseBoxAsync()
    {
        if (CurrentBox == null || !CanCloseBox) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Zamknięcie kartonu", 
            $"Czy na pewno chcesz zamknąć karton {CurrentBox.BoxCode}? Status zmieni się na 'Zamknięty'.", 
            "Tak", "Anuluj");

        if (confirm)
        {
            CurrentBox.Status = "Zamknięty";
            CurrentBox.IsClosed = true; 
    
            await _storageService.UpdateBox(CurrentBox);
            await _storageService.LogAudit(CurrentBox.BoxCode, "SYSTEM", 0, 0, "Zamknięcie kartonu");

            StatusMessage = $"Karton {CurrentBox.BoxCode} został zamknięty.";
    
            NotifyStateChanged();
            await RefreshCurrentBox(CurrentBox.BoxCode);
        }
    }
    [RelayCommand]
    private async Task ReopenBoxAsync()
    {
        if (CurrentBox == null) return;

 
        if (CurrentBox.Status == "Wysłany")
        {
            await Shell.Current.DisplayAlert("Błąd", "Nie można otworzyć kartonu, który został już wysłany.", "OK");
            return;
        }

        bool confirm = await Shell.Current.DisplayAlert(
            "Ponowne otwarcie", 
            $"Czy na pewno chcesz otworzyć karton {CurrentBox.BoxCode}? Status zmieni się na 'w komplementacji'.", 
            "Tak", "Anuluj");

        if (confirm)
        {
            CurrentBox.Status = "W kompletacji";
            CurrentBox.IsClosed = false; 

            await _storageService.UpdateBox(CurrentBox);
            await _storageService.LogAudit(CurrentBox.BoxCode, "SYSTEM", 0, 0, "Ponowne otwarcie kartonu");

            StatusMessage = $"Karton {CurrentBox.BoxCode} został ponownie otwarty.";

            NotifyStateChanged();
            await RefreshCurrentBox(CurrentBox.BoxCode);
        }
    }
    [RelayCommand]
    private async Task GoBackToMainAsync()
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}