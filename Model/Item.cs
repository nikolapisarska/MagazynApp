using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace MagazynApp.Model;

public partial class Item : ObservableObject
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    [ObservableProperty] private int _quantity;
    [ObservableProperty] private int _confirmedQuantity;
    [ObservableProperty] private int _missingQty;
    [ObservableProperty] private int _damagedQty;
    [ObservableProperty] private bool _isMissing;
    [ObservableProperty] private bool _isDamaged;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private bool _isFlagged; 

    [Ignore] public int Lp { get; set; }
    [Ignore] public bool IsEven { get; set; }

    // Właściwości obliczeniowe
    [Ignore] public string ExpectedVsConfirmed => $"{ConfirmedQuantity} / {Quantity}";
    
    [Ignore] public int RemainingToScan => Math.Max(0, Quantity - ConfirmedQuantity);

    [Ignore]
    public string StatusLabel 
    {
        get 
        {
            if (ConfirmedQuantity > Quantity) return "NADMIAR!";
            if (MissingQty > 0 || DamagedQty > 0) return "PROBLEM ZGŁOSZONY";
            return ConfirmedQuantity >= Quantity ? "KOMPLETNE" : $"POZOSTAŁO: {RemainingToScan}";
        }
    }

    [Ignore]
    public Color StatusColor => (ConfirmedQuantity > Quantity || MissingQty > 0 || DamagedQty > 0)
        ? Colors.Orange
        : (ConfirmedQuantity >= Quantity ? Colors.Green : Colors.White);

    // Pojedyncze wywołania powiadomień
    partial void OnQuantityChanged(int value) => RefreshProperties();
    partial void OnConfirmedQuantityChanged(int value) => RefreshProperties();
    partial void OnMissingQtyChanged(int value) => RefreshProperties();
    partial void OnDamagedQtyChanged(int value) => RefreshProperties();
    partial void OnIsMissingChanged(bool value) => RefreshProperties();
    partial void OnIsDamagedChanged(bool value) => RefreshProperties();
    partial void OnNotesChanged(string value) => RefreshProperties();
    
    public void RefreshProperties()
    {
        OnPropertyChanged(nameof(ExpectedVsConfirmed));
        OnPropertyChanged(nameof(RemainingToScan));
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(StatusColor));
    }
}