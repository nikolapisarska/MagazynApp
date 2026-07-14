using SQLite;

namespace MagazynApp.Model;

public class Product
{
    [PrimaryKey, AutoIncrement] // Ważne: AutoIncrement zapobiega nadpisywaniu
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CodeOrIdGraffiti { get; set; } = string.Empty;
  
    public  string Quantity { get; set; } = string.Empty;
    
    [Ignore] // Nie mapujemy tego do bazy
    public int Lp { get; set; } 
}