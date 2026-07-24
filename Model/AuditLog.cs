using SQLite;


namespace MagazynApp.Model;

public class AuditLog
{
    [PrimaryKey, AutoIncrement] 
    public int Id { get; set; }
    public string BoxCode { get; set; }
    public string Sku { get; set; }
    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }
    public string Reason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string Description => $"{Sku} | Zmiana: {OldQuantity} -> {NewQuantity} | Powód: {Reason}";
}