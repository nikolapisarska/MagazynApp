using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace MagazynApp.Model;

/// <summary>
/// Reprezentuje pojedynczą pozycję (produkt i jego ilość) wewnątrz konkretnego kartonu.
/// Dziedziczy po ObservableObject (CommunityToolkit.Mvvm) w celu automatycznego powiadamiania UI o zmianach.
/// </summary>
public partial class Item : ObservableObject
{
    [ObservableProperty] private string _codeOrIdGraffiti = string.Empty;
    [ObservableProperty] private string _productName = string.Empty;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(ExpectedVsConfirmed))]
    [NotifyPropertyChangedFor(nameof(RemainingToScan))]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    private int _quantity;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    private int _confirmedQuantity;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(ExpectedVsConfirmed))]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    private int _missingQty;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(ExpectedVsConfirmed))]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    private int _damagedQty;

    [ObservableProperty] private bool _isMissing;
    [ObservableProperty] private bool _isDamaged;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    private string _notes = string.Empty;

    [ObservableProperty] private bool _isFlagged; 

    // Ignorowane przez SQLite (właściwości pomocnicze dla interfejsu użytkownika)
    [Ignore] public int Lp { get; set; }
    [Ignore] public bool IsEven { get; set; }

    [Ignore] 
    public string ExpectedVsConfirmed => $"{Quantity - MissingQty - DamagedQty} / {Quantity}";
    
    [Ignore] 
    public int RemainingToScan => Math.Max(0, Quantity);

    [Ignore]
    public string StatusLabel 
    {
        get
        {
            var parts = new List<string>();
            if (MissingQty > 0) parts.Add($"BRAK ({MissingQty})");
            if (DamagedQty > 0) parts.Add($"USZK. ({DamagedQty})");

            if (parts.Count > 0)
                return $"{string.Join(" / ", parts)} | Do znalezienia: {MissingQty + DamagedQty}";
            return "KOMPLETNE";
        }
    }

    [Ignore]
    public Color StatusColor => (ConfirmedQuantity > Quantity || MissingQty > 0 || DamagedQty > 0)
        ? Colors.Orange
        : (ConfirmedQuantity >= Quantity ? Colors.Green : Colors.White);

    [Ignore] public bool ShouldBeDeleted { get; set; }

    /// <summary>Walidacja i reakcja na zmianę ilości produktu w pozycji.</summary>
    partial void OnQuantityChanged(int value)
    {
        if (value <= 0)
        {
            _quantity = 1;
            OnPropertyChanged(nameof(Quantity));
        }
        else
        {
            MissingQty = 0;
            DamagedQty = 0;
        
            if (string.IsNullOrEmpty(Notes))
            {
                IsFlagged = false;
            }
        }
    }

    partial void OnMissingQtyChanged(int value)
    {
        if (value < 0) { _missingQty = 0; OnPropertyChanged(nameof(MissingQty)); }
    }

    partial void OnDamagedQtyChanged(int value)
    {
        if (value < 0) { _damagedQty = 0; OnPropertyChanged(nameof(DamagedQty)); }
    }
}