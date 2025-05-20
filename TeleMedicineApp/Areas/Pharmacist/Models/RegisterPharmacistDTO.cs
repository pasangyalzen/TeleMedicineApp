using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace TeleMedicineApp.Areas.Admin.Models
{
    public class RegisterPharmacistDTO
    {
        [Required(ErrorMessage = "UserId is required.")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Full Name must be between 3 and 100 characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Pharmacy Name is required.")]
        [StringLength(150, ErrorMessage = "Pharmacy Name cannot exceed 150 characters.")]
        public string PharmacyName { get; set; }

        [Required(ErrorMessage = "License Number is required.")]
        [StringLength(50, ErrorMessage = "License Number cannot exceed 50 characters.")]
        public string LicenseNumber { get; set; }

        [Required(ErrorMessage = "Pharmacy Address is required.")]
        [StringLength(250, ErrorMessage = "Pharmacy Address cannot exceed 250 characters.")]
        public string PharmacyAddress { get; set; }

        [StringLength(100, ErrorMessage = "Working Hours cannot exceed 100 characters.")]
        public string WorkingHours { get; set; }

        [StringLength(500, ErrorMessage = "Services Offered cannot exceed 500 characters.")]
        public string ServicesOffered { get; set; }

        public IFormFile ProfileImage { get; set; }
        
    }
}