public class PrescriptionRequestDTO
{
    public int ConsultationId { get; set; }                   // The Consultation related to the prescription
    public List<PrescriptionItemRequestDTO> PrescriptionItems { get; set; }   // List of items in the prescription
}

public class PrescriptionItemRequestDTO
{
    public string MedicineName { get; set; }  // The name of the prescribed medicine
    public string Dosage { get; set; }        // Dosage of the medicine
    public string Frequency { get; set; }     // How often the medicine should be taken
    public string Duration { get; set; }      // Duration for how long the medicine should be taken
    public string Notes { get; set; }         // Additional notes related to the medicine
}