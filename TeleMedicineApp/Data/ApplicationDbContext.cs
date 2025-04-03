using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeleChaukiDB.Models;
//using TeleMedicineApp.Areas.Appointments.Models;
using TeleMedicineApp.Areas.Doctor.Models;
using TeleMedicineApp.Areas.Patient.Models;
using TeleMedicineApp.Areas.Pharmacist.Models;
using TeleMedicineApp.Models;

namespace TeleMedicineApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<DoctorDetails> DoctorDetails { get; set; }
    public DbSet<PatientDetails> PatientDetails { get; set; }
    public DbSet<Appointment> Appointments { get; set; }

    //public DbSet<PatientDetails> PatientDetails { get; set; }
    public DbSet<PharmacistDetails> PharmacistDetails { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<DoctorDetails>().ToTable("DoctorDetails"); // Already created in DB
    }
    
}