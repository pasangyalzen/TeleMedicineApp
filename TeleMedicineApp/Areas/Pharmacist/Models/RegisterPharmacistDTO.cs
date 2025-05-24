using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

public class RegisterPharmacistDTO
{
    [Required]
    public string UserId { get; set; }  // This should be the logged-in user's ID (from token or passed explicitly)

    [Required]
    public string FullName { get; set; }

    [Required]
    [Phone]
    public string PhoneNumber { get; set; }

    [Required]
    public string Gender { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    public string PharmacyName { get; set; }

    [Required]
    public string LicenseNumber { get; set; }

    [Required]
    public string PharmacyAddress { get; set; }

    public string WorkingHours { get; set; }

    public string ServicesOffered { get; set; }

    public IFormFile ProfileImage { get; set; }  // For profile photo upload

    // Optionally include doctorId and patientId if relevant, or omit if not
    public int? DoctorId { get; set; }
    public int? PatientId { get; set; }
}