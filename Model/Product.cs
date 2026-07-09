using System.ComponentModel.DataAnnotations.Schema;

namespace MagazynApp.Model;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CodeOrIdGraffiti { get; set; } = string.Empty;

    [NotMapped] 
    public int Lp { get; set; } 
}