namespace TeleMedicineApp.Areas.Pharmacist.ViewModels
{
    public class ConsultationPrescriptionViewModel
    {
        public int AppointmentId { get; set; }
        public string DoctorName { get; set; }
        public string PatientName { get; set; }
        public string Notes { get; set; }
        public string Recommendations { get; set; }
        public List<PrescriptionItemViewModel> PrescriptionItems { get; set; }
    }

    public class PrescriptionItemViewModel
    {
        public string MedicineName { get; set; }
        public string Dosage { get; set; }
        public string Frequency { get; set; }
        public string Duration { get; set; }
        public string Notes { get; set; }
    }
}