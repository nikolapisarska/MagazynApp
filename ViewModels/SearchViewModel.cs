using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagazynApp.Services;
using MagazynApp.Model;

namespace MagazynApp.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly IStorageService _storageService;
    private readonly NavigationState _navState;

    [ObservableProperty] private string _scanInput = string.Empty;
    [ObservableProperty] private string _statusMessage = "Zeskanuj kod kartonu, aby rozpocząć";

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    private Box? _currentBox;

    [ObservableProperty] private bool _isVerificationMode;

    public ObservableCollection<string> RecentScans { get; } = new();
    public bool IsEditable => CurrentBox != null && CurrentBox.Status != "Wysłany";

    public SearchViewModel(IStorageService storageService, NavigationState navState)
    {
        _storageService = storageService;
        _navState = navState;
    }

    [RelayCommand]
    private async Task AddProductAsync()
    {
        if (CurrentBox == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "BoxCode", CurrentBox.BoxCode },
            { "IsEditing", "true" } // Dodajemy flagę
        };

        await Shell.Current.GoToAsync(nameof(MainPage), navigationParameter);
    }
    [RelayCommand]
    private async Task ProcessScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanInput)) return;

        string codeToSearch = ScanInput.Trim();
        ScanInput = string.Empty;

        var box = await _storageService.GetBoxByCodeAsync(codeToSearch);

        if (box == null)
        {
            StatusMessage = "Karton nie istnieje w bazie.";
            await Shell.Current.DisplayAlert("Brak", "Karton nie istnieje w bazie.", "OK");
            return;
        }

        if (RecentScans.Contains(codeToSearch)) RecentScans.Remove(codeToSearch);
        RecentScans.Insert(0, codeToSearch);
        while (RecentScans.Count > 5) RecentScans.RemoveAt(5);

        CurrentBox = box;
        StatusMessage = $"Otwarto karton: {codeToSearch}";
    }
    [RelayCommand]
    private async Task EditQuantity(Item item)
    {
        if (!IsEditable) return;

        string? result = await Shell.Current.DisplayPromptAsync("Edytuj", "Podaj nową ilość",
            initialValue: item.Quantity.ToString(), keyboard: Keyboard.Numeric);

        if (int.TryParse(result, out int newQty))
        {
            int oldQty = item.Quantity;
            item.Quantity = newQty;
            await _storageService.LogAudit(CurrentBox!.BoxCode, item.ProductSku, oldQty, newQty, "Manualna korekta");
            await _storageService.UpdateBox(CurrentBox);
        }
    }

    [RelayCommand]
    private async Task ReportIssue(Item item)
    {
        string action = await Shell.Current.DisplayActionSheet("Rozbieżność", "Anuluj", null, "Zaginięcie", "Uszkodzenie");

        if (action == "Zaginięcie") item.IsMissing = true;
        else if (action == "Uszkodzenie") item.IsDamaged = true;
        else return;

        item.Notes = await Shell.Current.DisplayPromptAsync("Notatka", "Powód:");
        await _storageService.UpdateBox(CurrentBox!);
    }
    [RelayCommand]
    private async Task SaveAndReturnToMainAsync()
    {
        if (CurrentBox == null) return;

        // Opcjonalnie: zapisz zmiany w bazie przed przejściem
        await _storageService.UpdateBox(CurrentBox);

        // Przekazujemy ten sam kod kartonu, żeby MainPage wiedziało co załadować
        var navigationParameter = new Dictionary<string, object>
        {
            { "BoxCode", CurrentBox.BoxCode }
        };

        // Nawigacja "do przodu" do MainPage
        await Shell.Current.GoToAsync($"{nameof(MainPage)}", navigationParameter);
    }
}