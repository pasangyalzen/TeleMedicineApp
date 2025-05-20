using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

public class RegisterPatientDTO
{
    [Required]
    public string UserId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string FullName { get; set; }

    [Required]
    [Phone]
    public string PhoneNumber { get; set; }

    [Required]
    public string Gender { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }

    [StringLength(10)]
    public string BloodGroup { get; set; }

    [StringLength(200)]
    public string Address { get; set; }

    [StringLength(100)]
    public string EmergencyContactName { get; set; }

    [Phone]
    public string EmergencyContactNumber { get; set; }

    [StringLength(100)]
    public string HealthInsuranceProvider { get; set; }

    public string MedicalHistory { get; set; }

    public string MaritalStatus { get; set; }

    public string Allergies { get; set; }

    public string ChronicDiseases { get; set; }

    public string Medications { get; set; }

    public IFormFile ProfileImage { get; set; }
}