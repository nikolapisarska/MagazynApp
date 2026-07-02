using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using SQLite;
using SQLiteNetExtensions.Attributes; // Opcjonalna biblioteka do łatwych relacji

namespace MagazynApp.Model;
public class Box
{
    [PrimaryKey] 
    public string BoxCode { get; set; } = string.Empty;
    
    public float Height { get; set; }
    public float Width { get; set; }
    public float Length { get; set; }
    public float Weight { get; set; }

    [Ignore]
    public List<BoxItem> Items { get; set; } = new List<BoxItem>();
    
    public string ItemsJson { get; set; } = string.Empty;

    public string SyncStatus { get; set; } = "Pending"; 

    public void PrepareForSave()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        ItemsJson = JsonSerializer.Serialize(Items, options);
    }

    public void LoadAfterRead()
    {
        if (!string.IsNullOrEmpty(ItemsJson))
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            Items = JsonSerializer.Deserialize<List<BoxItem>>(ItemsJson, options) ?? new List<BoxItem>();
        }
        else
        {
            Items = new List<BoxItem>();
        }
    }
}

public class BoxItem : INotifyPropertyChanged
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; } 

    [Indexed] 
    public string BoxCode { get; set; } = string.Empty; 

    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;

    private int? _quantity = 1; 
    
    public int? Quantity
    {
        get => _quantity;
        set
        {
            int validatedValue = (value == null || value < 1) ? 1 : value.Value;
        
            if (_quantity != validatedValue)
            {
                _quantity = validatedValue;
                OnPropertyChanged(nameof(Quantity)); 
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}