using SQLite;
using System.Text.Json;

namespace MagazynApp.Model;

public class Item
{
    public string ProductId { get; set; }
    public string ProductSku { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    
    public int ConfirmedQuantity { get; set; }
    // Pola pomocnicze dla UI (nie muszą być w bazie)
    [Ignore]
    public int Lp { get; set; }
    [Ignore]
    public bool IsEven { get; set; }
    [Ignore]
    public string ExpectedVsConfirmed => $"{ConfirmedQuantity} / {Quantity}";
}

public class Box
{
    [PrimaryKey]
    public string BoxCode { get; set; } = string.Empty;
    public bool IsClosed { get; set; }

    [Ignore] 
    public List<Item> Items { get; set; } = new();

    // To pole jest widziane przez bazę jako tekst, ale nie używamy automatycznego gettera/settera
    public string ItemsJson { get; set; } = "[]"; 

    public void LoadAfterRead() 
    {
        // Ręczna deserializacja po pobraniu z bazy
        if (!string.IsNullOrEmpty(ItemsJson))
        {
            Items = JsonSerializer.Deserialize<List<Item>>(ItemsJson) ?? new List<Item>();
        }
    }

    public void PrepareForSave() 
    {
        // Ręczna serializacja przed zapisem do bazy
        ItemsJson = JsonSerializer.Serialize(Items);
    }
    public double Weight { get; set; }
    public double Length { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}