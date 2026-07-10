using SQLite;
using System.Text.Json;

namespace MagazynApp.Model;

public class Item
{
    public string ProductId { get; set; }
    public string ProductSku { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    
    // Pola pomocnicze dla UI (nie muszą być w bazie)
    [Ignore]
    public int Lp { get; set; }
    [Ignore]
    public bool IsEven { get; set; }
}

public class Box
{
    [PrimaryKey]
    public string BoxCode { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    
    [Ignore]
    public List<Item> Items { get; set; } = new();

    public string ItemsJson 
    { 
        get => JsonSerializer.Serialize(Items);
        set => Items = JsonSerializer.Deserialize<List<Item>>(value) ?? new List<Item>();
    }

    // Metody wymagane przez Twój ViewModel
    public void LoadAfterRead() { /* Dodatkowa logika po odczycie, jeśli potrzebna */ }
    public void PrepareForSave() { /* Dodatkowa logika przed zapisem, jeśli potrzebna */ }
}