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

/// <summary>
/// ViewModel odpowiedzialny za ekran weryfikacji, wyszukiwania kartonów, 
/// obsługę braków, uszkodzeń, notatek oraz zamykanie/ponowne otwieranie kartonów.
/// </summary>
[QueryProperty(nameof(ReloadBoxCode), "ReloadBoxCode")]
public partial class SearchViewModel(IStorageService storageService) : ObservableObject
{
    private readonly IStorageService _storageService = storageService;

    [ObservableProperty] private string _scanInput = string.Empty;
    [ObservableProperty] private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć";
    [ObservableProperty] private string? _reloadBoxCode;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    [NotifyPropertyChangedFor(nameof(HasBoxLoaded))] 
    [NotifyPropertyChangedFor(nameof(CanCloseBox))]
    private Box? _currentBox;

    public bool HasBoxLoaded => CurrentBox != null;
    public ObservableCollection<string> RecentScans { get; private set; } = [];
    
    public bool IsEditable => CurrentBox != null && 
                              CurrentBox.Status != BoxStatus.Sent && 
                              CurrentBox.Status != BoxStatus.Closed &&
                              !CurrentBox.IsClosed;

    /// <summary>Określa, czy karton spełnia warunki pozwalające na jego zamknięcie.</summary>
    public bool CanCloseBox => CurrentBox != null &&
                               CurrentBox.Status != BoxStatus.Sent &&
                               CurrentBox.Status != BoxStatus.Closed &&
                               CurrentBox.Items.Count != 0 &&
                               CurrentBox.Items.All(i => 
                                   (i.ConfirmedQuantity == i.Quantity || i.ConfirmedQuantity == 0) && 
                                   i.Quantity > 0 &&
                                   i.MissingQty == 0 && 
                                   i.DamagedQty == 0);

    partial void OnReloadBoxCodeChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = RefreshCurrentBox(value);
            ReloadBoxCode = null;
        }
    }

    /// <summary>Odświeża dane aktywnego kartonu z bazy danych i odświeża subskrypcje powiadomień.</summary>
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

    private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => NotifyStateChanged();

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(CanCloseBox));
        OnPropertyChanged(nameof(IsEditable));
    }

    [RelayCommand]
    private async Task AddProductAsync()
    {
        if (CurrentBox == null) return;
    
        await Shell.Current.GoToAsync($"{nameof(MainPage)}?BoxCode={CurrentBox.BoxCode}");
    }

    /// <summary>Przetwarza skanowanie w widoku wyszukiwania kartonu.</summary>
    [RelayCommand]
    private async Task ProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;
        string codeToSearch = ScanInput.Trim();
        ScanInput = string.Empty;

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
            if (RecentScans.Count > 5) RecentScans.RemoveAt(5);
        }

        WeakReferenceMessenger.Default.Send(new FocusScannerMessage());
    }

    /// <summary>Otwiera menu kontekstowe (popup) dla wybranego przedmiotu (braki, uszkodzenia, notatki, edycja).</summary>
    [RelayCommand]
    private async Task OpenIssuePopup(Item item)
    {
        if (!IsEditable || CurrentBox == null) return;

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
                await _storageService.UpdateBoxAsync(CurrentBox);
                await RefreshCurrentBox(CurrentBox.BoxCode);
            }
        }
        else if (action == "Edytuj ilość")
        {
            string? result = await Shell.Current.DisplayPromptAsync("Edytuj", "Podaj nową ilość (0, aby usunąć)", initialValue: item.Quantity.ToString(), keyboard: Keyboard.Numeric);
            if (int.TryParse(result, out int newQty))
            {
                newQty = Math.Max(0, newQty);
                int oldQty = item.Quantity;

                if (newQty == 0)
                {
                    bool confirm = await Shell.Current.DisplayAlertAsync("Usuwanie", $"Czy na pewno chcesz usunąć produkt {item.ProductName} z kartonu?", "Tak", "Anuluj");
                    if (!confirm) return;

                    CurrentBox.Items.Remove(item);
                    await _storageService.LogAuditAsync(CurrentBox.BoxCode, item.CodeOrIdGraffiti, oldQty, 0, "Usunięcie produktu (ilość 0)");
                }
                else
                {
                    item.Quantity = newQty;
                    await _storageService.LogAuditAsync(CurrentBox.BoxCode, item.CodeOrIdGraffiti, oldQty, newQty, "Manualna korekta ilości");
                }

                await _storageService.UpdateBoxAsync(CurrentBox);
                await RefreshCurrentBox(CurrentBox.BoxCode);
            }
        }
        else if (action is "Zgłoś braki" or "Zgłoś uszkodzenie")
        {
            string? result = await Shell.Current.DisplayPromptAsync(action, "Podaj ilość:", keyboard: Keyboard.Numeric);
            if (int.TryParse(result, out int qty) && qty > 0)
            {
                int dostepne = item.Quantity - item.MissingQty - item.DamagedQty;
                if (qty > dostepne) 
                { 
                    await Shell.Current.DisplayAlertAsync("Błąd", "Za duża ilość", "OK"); 
                    return; 
                }
        
                if (action == "Zgłoś braki") item.MissingQty += qty;
                else item.DamagedQty += qty;

                item.IsFlagged = true;
                await _storageService.UpdateBoxAsync(CurrentBox);
                await RefreshCurrentBox(CurrentBox.BoxCode);
            }
        }
        else if (action == "Wyczyść zgłoszenia")
        {
            item.MissingQty = 0;
            item.DamagedQty = 0;
            item.Notes = string.Empty;
            item.IsFlagged = false;

            await _storageService.UpdateBoxAsync(CurrentBox);
            await RefreshCurrentBox(CurrentBox.BoxCode);
        }
    }

    /// <summary>Uruchamia widok podsumowania weryfikacji.</summary>
    [RelayCommand]
    private async Task StartVerification()
    {
        if (CurrentBox == null)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Brak otwartego kartonu.", "OK");
            return;
        }

        var popup = new VerificationSummaryPopup(this);
        await Shell.Current.CurrentPage.ShowPopupAsync(popup);
    }

    /// <summary>Zamyka aktywny karton po spełnieniu wszystkich warunków weryfikacji.</summary>
    [RelayCommand]
    private async Task CloseBoxAsync()
    {
        if (CurrentBox == null || !CanCloseBox) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Zamknięcie kartonu", 
            $"Czy na pewno chcesz zamknąć karton {CurrentBox.BoxCode}? Status zmieni się na 'Zamknięty'.", 
            "Tak", "Anuluj");

        if (confirm)
        {
            CurrentBox.Status = BoxStatus.Closed;
            CurrentBox.IsClosed = true; 
    
            await _storageService.UpdateBoxAsync(CurrentBox);
            await _storageService.LogAuditAsync(CurrentBox.BoxCode, "SYSTEM", 0, 0, "Zamknięcie kartonu");

            StatusMessage = $"Karton {CurrentBox.BoxCode} został zamknięty.";
            await RefreshCurrentBox(CurrentBox.BoxCode);
        }
    }

    /// <summary>Ponownie otwiera zamknięty karton (zmienia status na w trakcie kompletacji).</summary>
    [RelayCommand]
    private async Task ReopenBoxAsync()
    {
        if (CurrentBox == null) return;

        if (CurrentBox.Status == BoxStatus.Sent)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Nie można otworzyć kartonu, który został już wysłany.", "OK");
            return;
        }

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Ponowne otwarcie", 
            $"Czy na pewno chcesz otworzyć karton {CurrentBox.BoxCode}? Status zmieni się na 'w kompletacji'.", 
            "Tak", "Anuluj");

        if (confirm)
        {
            CurrentBox.Status = BoxStatus.InProgress;
            CurrentBox.IsClosed = false; 

            await _storageService.UpdateBoxAsync(CurrentBox);
            await _storageService.LogAuditAsync(CurrentBox.BoxCode, "SYSTEM", 0, 0, "Ponowne otwarcie kartonu");

            StatusMessage = $"Karton {CurrentBox.BoxCode} został ponownie otwarty.";
            await RefreshCurrentBox(CurrentBox.BoxCode);
        }
    }

    [RelayCommand]
    private async Task GoBackToMainAsync() => await Shell.Current.GoToAsync("///DashboardPage");
}