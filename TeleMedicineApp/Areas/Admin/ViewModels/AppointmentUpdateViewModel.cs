namespace TeleMedicineApp.Areas.Admin.ViewModels;

public class AppointmentUpdateViewModel
{
    public int AppointmentId { get; set; } // Add AppointmentId
    public string DoctorName { get; set; }
    public string PatientName { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; }
    public string VideoCallLink { get; set; }
}