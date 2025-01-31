using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TeleMedicineApp.Data;
namespace TeleMedicineApp.Areas.Pharmacist.Models;

public class PharmacistDetails
{
    [Key]
    public int PharmacistId { get; set; } // Primary Key
    
    [Required]
    [ForeignKey("AspNetUsers")]
    public string UserId { get; set; } // Foreign Key to AspNetUsers table
    
    [MaxLength(255)]
    public string FullName { get; set; }
    
    [MaxLength(20)]
    public string PhoneNumber { get; set; }
    
    [MaxLength(10)]
    public string Gender { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    
    [MaxLength(255)]
    public string PharmacyName { get; set; }
    
    [MaxLength(100)]
    public string LicenseNumber { get; set; }
    
    [MaxLength(500)]
    public string PharmacyAddress { get; set; }
    
    [MaxLength(50)]
    public string WorkingHours { get; set; }
    
    [MaxLength(1000)]
    public string ServicesOffered { get; set; }
    
    [MaxLength(255)]
    public string ProfileImage { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation Property
    public virtual ApplicationUser AspNetUsers { get; set; } 
    
}