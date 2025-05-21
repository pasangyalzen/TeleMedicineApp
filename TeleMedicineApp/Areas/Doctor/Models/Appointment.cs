// using System;
// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;
//
// namespace TeleChaukiDB.Models
// {
//     public class Appointment
//     {
//         [Key]
//         public int AppointmentId { get; set; }
//
//         [Required]
//         [ForeignKey("Doctor")]
//         public int DoctorId { get; set; } // Reference to Doctor
//
//         [Required]
//         [ForeignKey("Patient")]
//         public int PatientId { get; set; } // Reference to Patient
//
//         [Required]
//         public DateTime ScheduledTime { get; set; } // Date and Time of Appointment
//
//         [Required]
//         [StringLength(50)]
//         public string Status { get; set; } // Example: "Pending", "Completed", "Cancelled"
//
//         // [StringLength(500)]
//         // public string? VideoCallLink { get; set; } // Link for video consultation
//
//         public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Auto-set creation date
//         public DateTime? UpdatedAt { get; set; } // Updated timestamp
//
//         public string AddedBy { get; set; } // ID of the user who created the appointment
//
//         
//     }
// }