using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TeleMedicineApp.Data;

namespace TeleMedicineApp.Areas.Patient.Models
{
    public class PatientDetails
    {
        [Key]
        public int PatientId { get; set; } // Primary Key

        [Required]
        public string UserId { get; set; } // Foreign Key - AspNetUsers

        [StringLength(255)]
        public string? FullName { get; set; }  

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; } 

        [StringLength(10)]
        public string? BloodGroup { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(255)]
        public string? EmergencyContactName { get; set; }

        [StringLength(20)]
        public string? EmergencyContactNumber { get; set; }

        [StringLength(255)]
        public string? HealthInsuranceProvider { get; set; }

        [StringLength(1000)]
        public string? MedicalHistory { get; set; }

        [StringLength(255)]
        public string? ProfileImage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Auto-set on creation

        // Foreign Key Navigation Property
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}