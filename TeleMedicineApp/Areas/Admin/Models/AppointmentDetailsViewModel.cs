namespace TeleMedicineApp.Areas.Admin.Models
{
    public class AppointmentDetailsViewModel
    {
        public string DoctorName { get; set; }  // Doctor's name
        public string PatientName { get; set; }  // Patient's name
        public DateTime ScheduledTime { get; set; }  // Scheduled time (without seconds and milliseconds)
        public string Status { get; set; }  // Appointment status
        public string VideoCallLink { get; set; }  // Link for video call
        
        // Method to ensure the ScheduledTime is normalized
        public void NormalizeScheduledTime()
        {
            // Normalize to hours and minutes, setting seconds and milliseconds to zero
            ScheduledTime = ScheduledTime.AddSeconds(-ScheduledTime.Second).AddMilliseconds(-ScheduledTime.Millisecond);
        }

        // Validate the AppointmentDetailsViewModel before saving
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(DoctorName) && 
                   !string.IsNullOrEmpty(PatientName) &&
                   ScheduledTime != DateTime.MinValue && 
                   !string.IsNullOrEmpty(Status) && 
                   !string.IsNullOrEmpty(VideoCallLink);
        }
    }
}