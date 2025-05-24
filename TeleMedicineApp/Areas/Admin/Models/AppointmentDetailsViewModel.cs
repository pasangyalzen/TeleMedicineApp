namespace TeleMedicineApp.Areas.Admin.Models
{
    public class AppointmentDetailsViewModel
    {
        public int DoctorId { get; set; }          // Doctor's ID
        public int PatientId { get; set; }         // Patient's ID
        public string? DoctorName { get; set; }     // (Optional) Display purpose
        public string? PatientName { get; set; }    // (Optional) Display purpose
        public DateTime AppointmentDate { get; set; } // Only the date (no time component)
        public TimeSpan StartTime { get; set; }    // Appointment start time
        public TimeSpan EndTime { get; set; }      // Appointment end time
        public string Status { get; set; }         // Appointment status (e.g., Scheduled, Completed)
        public string Reason { get; set; }         // Reason for appointment or video call link

        // Normalize to remove seconds and milliseconds
        public void NormalizeAppointmentTime()
        {
            StartTime = TimeSpan.FromMinutes(Math.Floor(StartTime.TotalMinutes));
            EndTime = TimeSpan.FromMinutes(Math.Floor(EndTime.TotalMinutes));
            AppointmentDate = AppointmentDate.Date;
        }

        // Validation method for appointment input
        public bool IsValid()
        {
            return DoctorId > 0 &&
                   PatientId > 0 &&
                   AppointmentDate != DateTime.MinValue &&
                   StartTime < EndTime &&
                   !string.IsNullOrEmpty(Status) &&
                   !string.IsNullOrEmpty(Reason);
        }
    }
}