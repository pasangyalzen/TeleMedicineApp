using System;
using System.ComponentModel.DataAnnotations;

namespace TeleMedicineApp.Areas.Doctor.Models
{
    public class DoctorAvailabilityDto
    {
        [Key]
        public int AvailabilityId { get; set; }

        [Required(ErrorMessage = "DoctorId is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "DoctorId must be a positive integer.")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "DayOfWeek is required.")]
        [Range(0, 6, ErrorMessage = "DayOfWeek must be between 0 (Sunday) and 6 (Saturday).")]
        public int DayOfWeek { get; set; }

        [Required(ErrorMessage = "StartTime is required.")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "EndTime is required.")]
        [DataType(DataType.Time)]
        [CustomValidation(typeof(DoctorAvailabilityDto), nameof(ValidateEndTime))]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "AppointmentDurationInMinutes is required.")]
        [Range(1, 1440, ErrorMessage = "Appointment duration must be between 1 and 1440 minutes.")]
        public int AppointmentDurationInMinutes { get; set; }

        [Required(ErrorMessage = "BufferTimeInMinutes is required.")]
        [Range(0, 1440, ErrorMessage = "Buffer time must be between 0 and 1440 minutes.")]
        public int BufferTimeInMinutes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Custom validation method to ensure EndTime is after StartTime
        public static ValidationResult ValidateEndTime(TimeSpan endTime, ValidationContext context)
        {
            var instance = context.ObjectInstance as DoctorAvailabilityDto;
            if (instance == null)
                return ValidationResult.Success;

            if (endTime <= instance.StartTime)
                return new ValidationResult("EndTime must be later than StartTime.");

            return ValidationResult.Success;
        }
    }
}