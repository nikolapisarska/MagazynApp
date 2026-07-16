using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace MagazynApp.Model;

public partial class Item : ObservableObject
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public bool IsValid => Quantity == ConfirmedQuantity;

    [ObservableProperty] private int _quantity;
    [ObservableProperty] private int _confirmedQuantity;
    [ObservableProperty] private bool _isMissing;
    [ObservableProperty] private bool _isDamaged;
    [ObservableProperty] private string _status = "OK";
    [ObservableProperty] private string _notes;

    // To pole jest wymagane przez DataTrigger w Twoim XAML, 
    // aby podświetlić wiersz na czerwono po zgłoszeniu problemu
    [ObservableProperty] private bool _isFlagged; 

    // Pola dla list
    [Ignore] public int Lp { get; set; }
    [Ignore] public bool IsEven { get; set; }

    [Ignore] public string ExpectedVsConfirmed => $"{ConfirmedQuantity} / {Quantity}";

    [Ignore]
    public string StatusLabel => IsMissing
        ? "BRAK"
        : (IsDamaged ? "USZKODZONY" : (ConfirmedQuantity >= Quantity ? "KOMPLETNE" : "W TRAKCIE"));

    [Ignore]
    public Color StatusColor => IsMissing
        ? Colors.Orange
        : (IsDamaged ? Colors.Red : (ConfirmedQuantity >= Quantity ? Colors.Green : Colors.White));

    // Metody częściowe dla aktualizacji UI
    partial void OnConfirmedQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(ExpectedVsConfirmed));
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(StatusColor));
    }

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(ExpectedVsConfirmed));
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(StatusColor));
    }

    partial void OnIsMissingChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(StatusColor));
    }

    partial void OnIsDamagedChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(StatusColor));
    }
}