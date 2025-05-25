using TeleMedicineApp.Attributes;

public class PrescriptionRequestDTO
{
    public int ConsultationId { get; set; }                   // The Consultation related to the prescription
    public List<PrescriptionItemRequestDTO> PrescriptionItems { get; set; }   // List of items in the prescription
}

public class PrescriptionItemRequestDTO
{
    [NoWhiteSpaceOnly]
    public string MedicineName { get; set; }  // The name of the prescribed medicine
    [NoWhiteSpaceOnly]
    public string Dosage { get; set; }        // Dosage of the medicine
    [NoWhiteSpaceOnly]
    public string Frequency { get; set; }     // How often the medicine should be taken
    [NoWhiteSpaceOnly]
    public string Duration { get; set; }      // Duration for how long the medicine should be taken
    [NoWhiteSpaceOnly]
    public string Notes { get; set; }         // Additional notes related to the medicine
    
}