using System.Text.Json;
using System.Text.Json.Serialization;

namespace MagazynApp.Model;

public class Box
{
    public string BoxCode { get; set; } = string.Empty;
    public float Height { get; set; }
    public float Width { get; set; }
    public float Length { get; set; }
    public float Weight { get; set; }
    
    // Ta lista jest używana w UI
    [JsonIgnore] 
    public List<BoxItem> Items { get; set; } = new List<BoxItem>();
    
    // To pole służy tylko do zapisu w pliku JSON
    public string ItemsJson { get; set; } = "[]";

    public void PrepareForSave()
    {
        ItemsJson = JsonSerializer.Serialize(Items);
    }

    public void LoadAfterRead()
    {
        if (!string.IsNullOrEmpty(ItemsJson))
        {
            Items = JsonSerializer.Deserialize<List<BoxItem>>(ItemsJson) ?? new List<BoxItem>();
        }
    }
    public class BoxItem
    {
        public string BoxCode { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }
}