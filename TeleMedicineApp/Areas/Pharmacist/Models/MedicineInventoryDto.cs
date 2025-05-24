public class InventoryUpdateDTO
{
    public int InventoryId { get; set; }
    public string MedicineName { get; set; }
    public int Quantity { get; set; }
    public string BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Manufacturer { get; set; }
    public decimal UnitPrice { get; set; }
}