using System;
using System.ComponentModel.DataAnnotations;

public class RegisterPatientDTO
{
    // Fields from AspNetUsers
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required, MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; }

    [Required, Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; }

    // Fields from PatientDetails
    [Required, StringLength(255)]
    public string FullName { get; set; }

    [Required, Phone]
    public string PhoneNumber { get; set; }

    [Required]
    [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be 'Male', 'Female', or 'Other'.")]
    public string Gender { get; set; }

    [Required]
    [DataType(DataType.Date)]
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
    public string?  HealthInsuranceProvider { get; set; }

    public string MedicalHistory { get; set; }

    [Url(ErrorMessage = "ProfileImage must be a valid URL.")]
    public string ProfileImage { get; set; }

    // Newly Added Fields
    [StringLength(50)]
    public string MaritalStatus { get; set; }

    public string Allergies { get; set; }

    public string ChronicDiseases { get; set; }

    public string Medications { get; set; }
}