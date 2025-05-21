namespace TeleMedicineApp.Areas.Doctor.Models;

public class RescheduleAppointmentRequest
{
    public int AppointmentId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }  // Use TimeSpan for SQL TIME
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = "Rescheduled";
}