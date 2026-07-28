using SQLite;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Encodings.Web;

namespace MagazynApp.Model;

/// <summary>
/// Reprezentuje karton (paczkę) w magazynie. Przechowuje wymiary, wagę, status oraz 
/// listę zawartych przedmiotów, które ze względów kompatybilności z bazą SQLite są serializowane do formatu JSON.
/// </summary>
public partial class Box : ObservableObject
{
    [PrimaryKey] public string BoxCode { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    
    [ObservableProperty] private string _status = BoxStatus.InProgress;

    private double _weight;
    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, Math.Abs(value)); // Zabezpieczenie przed ujemną wagą
    }

    private double _width;
    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, Math.Abs(value)); // Zabezpieczenie przed ujemną szerokością
    }

    private double _height;
    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, Math.Abs(value)); // Zabezpieczenie przed ujemną wysokością
    }

    private double _length;
    public double Length
    {
        get => _length;
        set => SetProperty(ref _length, Math.Abs(value)); // Zabezpieczenie przed ujemną długością
    }

    // Kolumna przechowująca listę produktów w bazie jako ciąg JSON
    
    public string ItemsJson { get; set; } = "[]";

    // Właściwość ignorowana przez bazę SQLite – reprezentuje kolekcję obiektów w pamięci aplikacji
    [Ignore] 
    public List<Item> Items { get; set; } = new();
    
    /// <summary>Deserializuje łańcuch JSON z bazy danych do obiektu listy przedmiotów.</summary>
    public void LoadAfterRead() 
    {
        Items = JsonSerializer.Deserialize<List<Item>>(ItemsJson) ?? new();
    }

    /// <summary>Serializuje listę przedmiotów do formatu JSON przed zapisem do bazy danych.</summary>
    public void PrepareForSave() 
    {
        var options = new JsonSerializerOptions 
        { 
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
        };
    
        ItemsJson = JsonSerializer.Serialize(Items, options);
    }
}
