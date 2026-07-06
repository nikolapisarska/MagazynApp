namespace MagazynApp.Model;
using SQLite;
using SQLiteNetExtensions.Attributes;

public class Product
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    [Indexed] // Dodajemy indeks, aby wyszukiwanie po kodzie było szybsze
    public string CodeOrIdGraffiti { get; set; } = string.Empty;
}