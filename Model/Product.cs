namespace MagazynApp.Model;
using SQLite;
using SQLiteNetExtensions.Attributes;
public class Product
{
    public string Name { get; set; } = string.Empty;
    public string CodeOrIdGraffiti { get; set; } = string.Empty;
}