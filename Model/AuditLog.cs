using SQLite;

namespace MagazynApp.Model;

public class AuditLog
{
    [PrimaryKey, AutoIncrement] 
    public int Id { get; set; }
    public string BoxCode { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [Ignore]
    public string Description => $"{Sku} | Zmiana: {OldQuantity} -> {NewQuantity} | Powód: {Reason}";
}