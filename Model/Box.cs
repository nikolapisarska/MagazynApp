using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SQLite;
using SQLiteNetExtensions.Attributes; // Opcjonalna biblioteka do łatwych relacji

namespace MagazynApp.Model;

public class Box
{
    [PrimaryKey] // Kod kreskowy kartonu musi być unikalnym kluczem
    public string BoxCode { get; set; } = string.Empty;
    
    public float Height { get; set; }
    public float Width { get; set; }
    public float Length { get; set; }
    public float Weight { get; set; }

    // Ignorujemy tę właściwość w czystym SQLite, ponieważ SQLite nie rozumie list.
    // Zawartość kartonu będziemy wyciągać z bazy danych po BoxCode.
    [Ignore]
    public List<BoxItem> Items { get; set; } = new List<BoxItem>();
    
    public string SyncStatus { get; set; } = "Pending"; 
}

public class BoxItem : INotifyPropertyChanged
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; } // Każdy element w kartonie potrzebuje swojego ID w bazie

    [Indexed] // Indeks przyspieszy wyszukiwanie wszystkich przedmiotów z danego kartonu
    public string BoxCode { get; set; } = string.Empty; // Klucz obcy łączący produkt z kartonem

    // SQLite nie zapisze całego obiektu Product w jednej komórce. 
    // Zapisujemy ID produktu oraz jego nazwę/indeks bezpośrednio w tej tabeli.
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;

    private int? _quantity = 1; 
    
    public int? Quantity
    {
        get => _quantity;
        set
        {
            int validatedValue = (value == null || value < 1) ? 1 : value.Value;
        
            if (_quantity != validatedValue)
            {
                _quantity = validatedValue;
                OnPropertyChanged(nameof(Quantity)); 
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}