using System.ComponentModel.DataAnnotations.Schema;
using SQLite;

namespace MagazynApp.Model;

public class Product
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CodeOrIdGraffiti { get; set; } = string.Empty;

    [NotMapped] 
    public int Lp { get; set; } 
}