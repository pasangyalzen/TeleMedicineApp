using TeleMedicineApp.Attributes;
using TeleMedicineApp.Models;

public class ConsultationCreateDTO
{
    public int AppointmentId { get; set; }
    [NoWhiteSpaceOnly]
    public string? Notes { get; set; }
    [NoWhiteSpaceOnly]
    public string? Recommendations { get; set; }
}