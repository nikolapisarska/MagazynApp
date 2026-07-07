using System.Text.Json;
using System.Text.Json.Serialization;

namespace MagazynApp.Model;

public class Box
{
    private float _height;
    private float _width;
    private float _length;
    private float _weight;

    public string BoxCode { get; set; } = string.Empty;

    public float Height
    {
        get => _height;
        set => _height = Math.Abs(value); // Zawsze zamieni na wartość dodatnią
    }

    public float Width
    {
        get => _width;
        set => _width = Math.Abs(value);
    }

    public float Length
    {
        get => _length;
        set => _length = Math.Abs(value);
    }

    public float Weight
    {
        get => _weight;
        set => _weight = Math.Abs(value);
    }
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