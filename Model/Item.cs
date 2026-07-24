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

    // Właściwość obliczeniowa: x = Quantity - MissingQty - DamagedQty, y = Quantity
    [Ignore] 
    public string ExpectedVsConfirmed => $"{Quantity - MissingQty - DamagedQty} / {Quantity}";
    
    [Ignore] 
    public int RemainingToScan => Math.Max(0, Quantity - ConfirmedQuantity);

    [Ignore]
    public string StatusLabel 
    {
        get 
        {
            var parts = new List<string>();
            if (MissingQty > 0) parts.Add($"BRAK ({MissingQty})");
            if (DamagedQty > 0) parts.Add($"USZK. ({DamagedQty})");
            
            if (parts.Count > 0)
                return $"{string.Join(" / ", parts)} | Do znalezienia: {MissingQty+DamagedQty}";
            
            if (ConfirmedQuantity > Quantity) return "NADMIAR!";
            return ConfirmedQuantity >= Quantity ? "KOMPLETNE" : $"POZOSTAŁO: {RemainingToScan}";
        }
    }

    [Ignore]
    public Color StatusColor => (ConfirmedQuantity > Quantity || MissingQty > 0 || DamagedQty > 0)
        ? Colors.Orange
        : (ConfirmedQuantity >= Quantity ? Colors.Green : Colors.White);

    // Pojedyncze wywołania powiadomień
    // Właściwość pomocnicza do informowania o chęci usunięcia elementu
    [Ignore] public bool ShouldBeDeleted { get; set; }

    partial void OnQuantityChanged(int value)
    {
        if (value < 0)
        {
            _quantity = 0;
            OnPropertyChanged(nameof(Quantity));
        }
        else if (value == 0)
        {
            ShouldBeDeleted = true;
        }
        else
        {
            // Po zmianie ilości resetujemy zgłoszone braki i uszkodzenia,
            // ponieważ kontekst ilościowy uległ zmianie.
            MissingQty = 0;
            DamagedQty = 0;
        
            // Jeśli nie ma też notatek, wyłączamy flagę ostrzeżenia
            if (string.IsNullOrEmpty(Notes))
            {
                IsFlagged = false;
            }
        }

        RefreshProperties();
    }

    partial void OnMissingQtyChanged(int value)
    {
        if (value < 0) { _missingQty = 0; OnPropertyChanged(nameof(MissingQty)); }
        RefreshProperties();
    }

    partial void OnDamagedQtyChanged(int value)
    {
        if (value < 0) { _damagedQty = 0; OnPropertyChanged(nameof(DamagedQty)); }
        RefreshProperties();
    }
    partial void OnIsMissingChanged(bool value) => RefreshProperties();
    partial void OnIsDamagedChanged(bool value) => RefreshProperties();
    partial void OnNotesChanged(string value) => RefreshProperties();
        partial void OnConfirmedQuantityChanged(int value) => RefreshProperties();
    public void RefreshProperties()
    {
        OnPropertyChanged(nameof(ExpectedVsConfirmed));
        OnPropertyChanged(nameof(RemainingToScan));
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(StatusColor));
    }
    
}