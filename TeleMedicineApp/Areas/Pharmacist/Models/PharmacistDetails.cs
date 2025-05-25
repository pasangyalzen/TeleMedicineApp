using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TeleMedicineApp.Data;

namespace TeleMedicineApp.Areas.Pharmacist.Models
{
    public class PharmacistDetails
    {
        [Key]
        public int PharmacistId { get; set; }

        [Required(ErrorMessage = "User ID is required.")]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(255, ErrorMessage = "Full Name cannot be longer than 255 characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        [StringLength(20, ErrorMessage = "Phone number cannot be longer than 20 characters.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        [StringLength(10, ErrorMessage = "Gender cannot be longer than 10 characters.")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        [Column(TypeName = "datetime2")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Pharmacy Name is required.")]
        [StringLength(255, ErrorMessage = "Pharmacy Name cannot be longer than 255 characters.")]
        public string PharmacyName { get; set; }

        [Required(ErrorMessage = "License Number is required.")]
        [StringLength(100, ErrorMessage = "License Number cannot be longer than 100 characters.")]
        public string LicenseNumber { get; set; }

        [Required(ErrorMessage = "Pharmacy Address is required.")]
        [StringLength(500, ErrorMessage = "Pharmacy Address cannot be longer than 500 characters.")]
        public string PharmacyAddress { get; set; }

        [StringLength(100, ErrorMessage = "Working Hours cannot be longer than 100 characters.")]
        public string WorkingHours { get; set; }

        [StringLength(1000, ErrorMessage = "Services Offered cannot be longer than 1000 characters.")]
        public string ServicesOffered { get; set; }

        [StringLength(500, ErrorMessage = "Profile Image path cannot be longer than 500 characters.")]
        public string ProfileImage { get; set; }

        [Required(ErrorMessage = "Creation date is required.")]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Updated date is required.")]
        [Column(TypeName = "datetime2")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool Status { get; set; } = true;

        // Optional foreign keys
        public int? DoctorId { get; set; }
        public int? PatientId { get; set; }

        // Navigation properties (if you have Doctor and Patient entities)
        // [ForeignKey("DoctorId")]
        // public DoctorDetails Doctor { get; set; }
        
        // [ForeignKey("PatientId")]
        // public PatientDetails Patient { get; set; }
    }
}