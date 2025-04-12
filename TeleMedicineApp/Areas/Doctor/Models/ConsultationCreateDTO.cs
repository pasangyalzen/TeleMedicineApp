using TeleChaukiDB.Models;

public class ConsultationCreateDTO
{
    public int AppointmentId { get; set; }
    public string? Notes { get; set; }
    public string? Recommendations { get; set; }
}