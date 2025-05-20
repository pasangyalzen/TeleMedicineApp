using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System;
using TeleMedicineApp.Attributes;

public class RegisterDoctorDTO
{
    
    [Required(ErrorMessage = "User ID is required.")]
    public string UserId { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100)]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    public string PhoneNumber { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    public string Gender { get; set; }

    [Required(ErrorMessage = "Date of birth is required.")]
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "License number is required.")]
    [StringLength(50)]
    public string LicenseNumber { get; set; }

    [Required(ErrorMessage = "Medical college is required.")]
    public string MedicalCollege { get; set; }

    [Required(ErrorMessage = "Specialization is required.")]
    public string Specialization { get; set; }

    [Required(ErrorMessage = "Years of experience is required.")]
    [Range(0, 100, ErrorMessage = "Years of experience must be between 0 and 100.")]
    public int YearsOfExperience { get; set; }

    [Required(ErrorMessage = "Clinic name is required.")]
    public string ClinicName { get; set; }

    [Required(ErrorMessage = "Clinic address is required.")]
    public string ClinicAddress { get; set; }

    [Required(ErrorMessage = "Consultation fee is required.")]
    [Range(0.0, 10000.0, ErrorMessage = "Consultation fee must be between 0 and 10,000.")]
    public decimal ConsultationFee { get; set; }

    [Required(ErrorMessage = "Profile image is required.")]
    [AllowedExtensions(new[] { ".jpg", ".jpeg", ".png" })]
    public IFormFile ProfileImage { get; set; }
}