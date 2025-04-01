using System.ComponentModel.DataAnnotations;
using TeleMedicineApp.Data;

namespace TeleMedicineApp.Areas.Pharmacist.ViewModels
{
    public class PharmacistUpdateViewModel
    {
        [Key]
        public int PharmacistId { get; set; }
        public string Email { get; set; }
        
        [StringLength(255, ErrorMessage = "Full Name cannot be longer than 255 characters.")]
        public string FullName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
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

        [StringLength(50, ErrorMessage = "Working Hours cannot be longer than 50 characters.")]
        public string WorkingHours { get; set; }

        [StringLength(1000, ErrorMessage = "Services Offered cannot be longer than 1000 characters.")]
        public string ServicesOffered { get; set; }

        public string ProfileImage { get; set; }

        
    }
}