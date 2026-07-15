using SQLite;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MagazynApp.Model;

public partial class Box : ObservableObject
{
    [PrimaryKey] public string BoxCode { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    
    [ObservableProperty] private string _status = "W kompletacji";
    
    // Dodano pole wagi z atrybutem ObservableProperty
    [ObservableProperty] private double _weight = 0.0; 
    
    public string ItemsJson { get; set; } = "[]";

    [Ignore] public List<Item> Items { get; set; } = new();

    public void LoadAfterRead() => Items = JsonSerializer.Deserialize<List<Item>>(ItemsJson) ?? new();
    public void PrepareForSave() => ItemsJson = JsonSerializer.Serialize(Items);
}