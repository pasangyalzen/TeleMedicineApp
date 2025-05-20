using System.Text.Json.Serialization;
using TeleChaukiDB.Models;

public class Consultation
{
    public int ConsultationId { get; set; }
    public int AppointmentId { get; set; }
    public string Notes { get; set; }
    public string Recommendations { get; set; }
    public DateTime CreatedAt { get; set; }

    public Appointment Appointment { get; set; }
    [JsonIgnore]
    public ICollection<Prescription> Prescriptions { get; set; }
}