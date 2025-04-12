public class Prescription
{
    public int PrescriptionId { get; set; }
    public int ConsultationId { get; set; }
    public DateTime PrescribedAt { get; set; }

    public Consultation Consultation { get; set; }
    public ICollection<PrescriptionItem> PrescriptionItems { get; set; }
}