using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MagazynApp.Model;

public partial class Box
{
    private float _height;
    private float _width;
    private float _length;
    private float _weight;

    public string BoxCode { get; set; } = string.Empty;

    public float Height { get => _height; set => _height = Math.Abs(value); }
    public float Width { get => _width; set => _width = Math.Abs(value); }
    public float Length { get => _length; set => _length = Math.Abs(value); }
    public float Weight { get => _weight; set => _weight = Math.Abs(value); }

    [JsonIgnore] 
    public List<BoxItem> Items { get; set; } = new List<BoxItem>();
    
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

    public partial class BoxItem : ObservableObject
    {
        public string BoxCode { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;

        [ObservableProperty]
        private int _quantity = 1;

        // Nowe pola
        [ObservableProperty]
        private int _lp;

        [ObservableProperty]
        private bool _isEven;
    }
}