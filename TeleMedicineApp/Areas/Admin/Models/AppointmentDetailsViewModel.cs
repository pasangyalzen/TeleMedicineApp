namespace TeleMedicineApp.Areas.Admin.Models;

public class AppointmentDetailsViewModel
{
    //public int AppointmentId { get; set; }
    public string DoctorName { get; set; } // Add DoctorName
    public string PatientName { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; }
    public string VideoCallLink { get; set; }
    //public DateTime CreatedAt { get; set; }
    //public DateTime UpdatedAt { get; set; }
}