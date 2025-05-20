using System.Text.Json.Serialization;

public class Prescription
{
    public int PrescriptionId { get; set; }
    public int ConsultationId { get; set; }
    public DateTime PrescribedAt { get; set; }
    [JsonIgnore] 
    public Consultation Consultation { get; set; }
    [JsonIgnore]
    public ICollection<PrescriptionItem> PrescriptionItems { get; set; }
}