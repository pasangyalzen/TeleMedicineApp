using System;
using System.ComponentModel.DataAnnotations;
using TeleMedicineApp.Data;

namespace TeleMedicineApp.Areas.Patient.Models
{
    public class PatientDetailsViewModel
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        public string UserId { get; set; }
        //public ApplicationUser User { get; set; }

        [Required, StringLength(255)]
        public string FullName { get; set; }

        [Required, Phone]
        public string PhoneNumber { get; set; }

        [Required]
        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be 'Male', 'Female', or 'Other'.")]
        public string Gender { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

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

        // Newly Added Fields
        [StringLength(50)]
        public string MaritalStatus { get; set; }

        public string Allergies { get; set; }
        public string ChronicDiseases { get; set; }
        public string Medications { get; set; }

        public bool Status { get; set; } = true; // Active by default
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
    }
}