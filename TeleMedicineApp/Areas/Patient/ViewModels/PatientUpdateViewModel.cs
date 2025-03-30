using System;
using System.ComponentModel.DataAnnotations;

namespace TeleMedicineApp.Areas.Patient.ViewModels
{
    public class PatientUpdateViewModel
    {
        public int PatientId { get; set; }
        public string Email { get; set; }
        [StringLength(255)]
        public string FullName { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be 'Male', 'Female', or 'Other'.")]
        public string Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string BloodGroup { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        [StringLength(255)]
        public string EmergencyContactName { get; set; }

        [Phone]
        public string EmergencyContactNumber { get; set; }

        [StringLength(255)]
        public string HealthInsuranceProvider { get; set; }

        public string MedicalHistory { get; set; }

        [Url]
        public string ProfileImage { get; set; }

        [StringLength(50)]
        public string MaritalStatus { get; set; }

        public string Allergies { get; set; }
        public string ChronicDiseases { get; set; }
        public string Medications { get; set; }

        public bool? Status { get; set; } = true; // Active by default
    }
}