using SQLite;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Encodings.Web;

namespace MagazynApp.Model;

public partial class Box : ObservableObject
{
    [PrimaryKey] public string BoxCode { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    
    [ObservableProperty] private string _status = "W kompletacji";

    private double _weight;
    public double Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, Math.Abs(value));
    }

    private double _width;
    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, Math.Abs(value));
    }

    private double _height;
    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, Math.Abs(value));
    }

    private double _length;
    public double Length
    {
        get => _length;
        set => SetProperty(ref _length, Math.Abs(value));
    }

    public string ItemsJson { get; set; } = "[]";

    [Ignore] 
    public List<Item> Items { get; set; } = new();
    
    public void SyncItems() 
    {
        ItemsJson = JsonSerializer.Serialize(Items);
    }
    
    public void LoadAfterRead() 
    {
        Items = JsonSerializer.Deserialize<List<Item>>(ItemsJson) ?? new();
    }

    public void PrepareForSave() 
    {
        var options = new JsonSerializerOptions 
        { 
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
        };
    
        ItemsJson = JsonSerializer.Serialize(Items, options);
    }
}