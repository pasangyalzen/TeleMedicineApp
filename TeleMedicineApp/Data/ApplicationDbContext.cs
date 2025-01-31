using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeleMedicineApp.Areas.Doctor.Models;
using TeleMedicineApp.Models;

namespace TeleMedicineApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<DoctorDetails> DoctorDetails { get; set; }
    //public DbSet<PatientDetails> PatientDetails { get; set; }
    //public DbSet<PharmacistDetails> PharmacistDetails { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<DoctorDetails>().ToTable("DoctorDetails"); 
        
    }
    
}