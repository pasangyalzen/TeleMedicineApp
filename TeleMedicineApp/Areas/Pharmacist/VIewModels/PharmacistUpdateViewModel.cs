using System.ComponentModel.DataAnnotations;

namespace TeleMedicineApp.Areas.Pharmacist.ViewModels
{
    public class PharmacistUpdateViewModel
    {
        public int PharmacistId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot be longer than 100 characters.")]
        public string FullName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Pharmacy Name is required.")]
        [StringLength(150, ErrorMessage = "Pharmacy Name cannot be longer than 150 characters.")]
        public string PharmacyName { get; set; }

        [Required(ErrorMessage = "License Number is required.")]
        [StringLength(50, ErrorMessage = "License Number cannot be longer than 50 characters.")]
        public string LicenseNumber { get; set; }

        [Required(ErrorMessage = "Pharmacy Address is required.")]
        [StringLength(250, ErrorMessage = "Pharmacy Address cannot be longer than 250 characters.")]
        public string PharmacyAddress { get; set; }

        [StringLength(100, ErrorMessage = "Working Hours cannot be longer than 100 characters.")]
        public string WorkingHours { get; set; }

        [StringLength(500, ErrorMessage = "Services Offered cannot be longer than 500 characters.")]
        public string ServicesOffered { get; set; }

        public string ProfileImage { get; set; }

        [Required(ErrorMessage = "Updated date is required.")]
        public DateTime UpdatedAt { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        public int? DoctorId { get; set; }  // Optional: Based on your table
        public int? PatientId { get; set; }  // Optional: Based on your table
    }
}