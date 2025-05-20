using System.Text.Json.Serialization;

public class PrescriptionItem
{
    public int PrescriptionItemId { get; set; }
    public int PrescriptionId { get; set; }
    public string MedicineName { get; set; }
    public string Dosage { get; set; }
    public string Frequency { get; set; }
    public string Duration { get; set; }
    public string Notes { get; set; }
    [JsonIgnore]
    public Prescription Prescription { get; set; }
}   