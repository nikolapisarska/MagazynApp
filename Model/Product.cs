using SQLite;

namespace MagazynApp.Model;

public class Product
{
    [PrimaryKey, AutoIncrement] 
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CodeOrIdGraffiti { get; set; } = string.Empty;
  
    public int DefaultQuantity { get; set; } = 1;
    
    [Ignore]
    public int Lp { get; set; } 
}