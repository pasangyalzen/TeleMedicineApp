
using System.ComponentModel.DataAnnotations;

namespace TeleMedicineApp.Areas.Pharmacist.VIewModels;

public class PharmacistRegistrationViewModel
{
        public int? PharmacistId { get;} // Optional for editing; not needed for creation.

        [Required(ErrorMessage = "Full Name is required.")]
        [MaxLength(255, ErrorMessage = "Full Name cannot exceed 255 characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [MaxLength(20, ErrorMessage = "Phone Number cannot exceed 20 characters.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        [MaxLength(10, ErrorMessage = "Gender cannot exceed 10 characters.")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date, ErrorMessage = "Invalid date format.")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Pharmacy Name is required.")]
        [MaxLength(255, ErrorMessage = "Pharmacy Name cannot exceed 255 characters.")]
        public string PharmacyName { get; set; }

        [Required(ErrorMessage = "License Number is required.")]
        [MaxLength(100, ErrorMessage = "License Number cannot exceed 100 characters.")]
        public string LicenseNumber { get; set; }

        [MaxLength(500, ErrorMessage = "Pharmacy Address cannot exceed 500 characters.")]
        public string PharmacyAddress { get; set; }

        [MaxLength(50, ErrorMessage = "Working Hours cannot exceed 50 characters.")]
        public string WorkingHours { get; set; }

        [MaxLength(1000, ErrorMessage = "Services Offered cannot exceed 1000 characters.")]
        public string ServicesOffered { get; set; }

        [DataType(DataType.Upload)]
        public string ProfileImage { get; set; } 
}