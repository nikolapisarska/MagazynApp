using SQLite;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MagazynApp.Model;


public partial class Item : ObservableObject
{
    public string ProductId { get; set; }
    public string ProductSku { get; set; }
    public string ProductName { get; set; }
    
    [ObservableProperty] private int _quantity;
    [ObservableProperty] private int _confirmedQuantity;
    [ObservableProperty] private bool _isMissing;
    [ObservableProperty] private bool _isDamaged;
    
    // Pola dla list
    [Ignore] public int Lp { get; set; }
    [Ignore] public bool IsEven { get; set; }

    [Ignore] public string ExpectedVsConfirmed => $"{ConfirmedQuantity} / {Quantity}";
    [Ignore] public string StatusLabel => IsMissing ? "BRAK" : (IsDamaged ? "USZKODZONY" : (ConfirmedQuantity >= Quantity ? "KOMPLETNE" : "W TRAKCIE"));
    [Ignore] public Color StatusColor => IsMissing ? Colors.Orange : (IsDamaged ? Colors.Red : (ConfirmedQuantity >= Quantity ? Colors.Green : Colors.White));
}

public class Box
{
    [PrimaryKey] public string BoxCode { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public string ItemsJson { get; set; } = "[]";

    [Ignore] public List<Item> Items { get; set; } = new();

    public void LoadAfterRead() => Items = JsonSerializer.Deserialize<List<Item>>(ItemsJson) ?? new();
    public void PrepareForSave() => ItemsJson = JsonSerializer.Serialize(Items);
}