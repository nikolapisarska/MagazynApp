using SQLite;

namespace MagazynApp.Model;

/// <summary>
/// Reprezentuje słownikowy produkt dostępny w systemie magazynowym.
/// Zawiera informacje o nazwie, unikalnym kodzie/ID Graffiti oraz domyślnej ilości.
/// </summary>
public class Product
{
    [PrimaryKey, AutoIncrement] 
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CodeOrIdGraffiti { get; set; } = string.Empty;
    public int DefaultQuantity { get; set; } = 1;
    
    // Pole pomocnicze ignorowane przez SQLite (używane np. do wyświetlania numeracji w tabelach UI)
    [Ignore]
    public int Lp { get; set; } 
}