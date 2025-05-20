using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TeleMedicineApp.Areas.Doctor.Models;
using TeleMedicineApp.Areas.Patient.Models;

public class Appointment
{
    [Key]
    public int AppointmentId { get; set; }

    [ForeignKey("Doctor")]
    public int DoctorId { get; set; }

    [ForeignKey("Patient")]
    public int PatientId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(255)]
    public string? AddedBy { get; set; }

    public DateTime? AppointmentDate { get; set; }

    public TimeSpan? StartTime { get; set; }

    public TimeSpan? EndTime { get; set; }

    public int? AppointmentDurationInMinutes { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public virtual DoctorDetails Doctor { get; set; }
    public virtual PatientDetails Patient { get; set; }
}