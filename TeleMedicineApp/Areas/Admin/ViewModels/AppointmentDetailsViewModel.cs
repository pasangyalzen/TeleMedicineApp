namespace TeleMedicineApp.Areas.Admin.ViewModels;

public class AppointmentDetailsViewModel
{
    public int AppointmentId { get; set; }
    public int DoctorId { get; set; }
    public int PatientId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; }
    public string VideoCallLink { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
                
    // You can include additional properties here, like the doctor's and patient's names, if needed.
    // public string DoctorName { get; set; }
    // public string PatientName { get; set; }
}