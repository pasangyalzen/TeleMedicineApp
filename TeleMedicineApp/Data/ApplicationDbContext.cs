using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeleMedicineApp.Models;
using TeleMedicineApp.Areas.Doctor.Models;
using TeleMedicineApp.Areas.Patient.Models;
using TeleMedicineApp.Areas.Pharmacist.Models;
using TeleMedicineApp.Models;

namespace TeleMedicineApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        // DbSets for all entities
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<DoctorDetails> DoctorDetails { get; set; }
        public DbSet<PatientDetails> PatientDetails { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<PharmacistDetails> PharmacistDetails { get; set; }

        // Adding new DbSets for Consultation, Prescription, and PrescriptionItem
        public DbSet<Consultation> Consultations { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
        public DbSet<DoctorAvailability> DoctorAvailability { get; set; }
        
        // Optional: You can add additional configurations for relationships or table names here if necessary.
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Custom table name mappings if needed
            modelBuilder.Entity<DoctorDetails>().ToTable("DoctorDetails"); // Already created in DB
            modelBuilder.Entity<PatientDetails>().ToTable("PatientDetails"); // Example for patient details
            modelBuilder.Entity<PharmacistDetails>().ToTable("PharmacistDetails"); // Example for pharmacist details
            
            // Define additional relationships or constraints if necessary
            // For example, you can specify the foreign key relationships explicitly.
            
            modelBuilder.Entity<Consultation>()
                .HasOne(c => c.Appointment)
                .WithMany()  // Since Appointment can have many consultations (if applicable)
                .HasForeignKey(c => c.AppointmentId);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Consultation)
                .WithMany(c => c.Prescriptions)
                .HasForeignKey(p => p.ConsultationId);
                
            modelBuilder.Entity<PrescriptionItem>()
                .HasOne(pi => pi.Prescription)
                .WithMany(p => p.PrescriptionItems)
                .HasForeignKey(pi => pi.PrescriptionId);
        }
    }
}