namespace TeleMedicineApp.Areas.Pharmacist.Models;

public class MedicineRequest
{
    public int RequestId { get; set; }
    public int PrescriptionId { get; set; }
    public string DeliveryAddress { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string PaymentMethod { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}