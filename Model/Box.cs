using SQLite;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MagazynApp.Model;




public class Box
{
    [PrimaryKey] public string BoxCode { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public string ItemsJson { get; set; } = "[]";

    [Ignore] public List<Item> Items { get; set; } = new();

    public void LoadAfterRead() => Items = JsonSerializer.Deserialize<List<Item>>(ItemsJson) ?? new();
    public void PrepareForSave() => ItemsJson = JsonSerializer.Serialize(Items);
}