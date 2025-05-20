using System.ComponentModel.DataAnnotations;

public class DoctorAvailability
{
    [Key]
    public int AvailabilityId { get; set; }
    public int DoctorId { get; set; }   
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    // public bool IsAvailable { get; set; }
    public int AppointmentDurationInMinutes { get; set; }
    public int BufferTimeInMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}