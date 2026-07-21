using SQLite;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MagazynApp.Model;

public partial class Box : ObservableObject
{
    [PrimaryKey] public string BoxCode { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    
    [ObservableProperty] private string _status = "W kompletacji";
    
    // Pole wagi
    [ObservableProperty] private double _weight = 0.0 ; 
    [ObservableProperty] private double _width = 0.0;
    [ObservableProperty] private double _height = 0.0;
    [ObservableProperty] private double _length = 0.0;

    public string ItemsJson { get; set; } = "[]";

    [Ignore] 
    public List<Item> Items { get; set; } = new();
    
    public void SyncItems() 
    {
        ItemsJson = JsonSerializer.Serialize(Items);
    }
    
    // Pobieranie danych z bazy (uwzględnia deserializację stanu, jeśli trzymasz go w JSON lub osobnych kolumnach)
    public void LoadAfterRead() 
    {
        Items = JsonSerializer.Deserialize<List<Item>>(ItemsJson) ?? new();
    }
    
    // Przygotowanie do zapisu - upewnij się, że wszystko jest zserializowane
    public void PrepareForSave() 
    {
        ItemsJson = JsonSerializer.Serialize(Items);
    }
}