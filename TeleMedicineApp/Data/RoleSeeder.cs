using Microsoft.AspNetCore.Identity;

namespace TeleMedicineApp.Data;

public static class RoleSeeder
{
    public static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "SuperAdmin", "Admin", "Doctor", "Patient", "Pharmacist" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public static async Task SeedSuperAdmin(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        // Create SuperAdmin user
        var superAdminEmail = configuration["SuperAdmin:Email"];
        var superAdminPassword = configuration["SuperAdmin:Password"];

        if (string.IsNullOrEmpty(superAdminEmail) || string.IsNullOrEmpty(superAdminPassword))
        {
            throw new Exception("SuperAdmin credentials not configured");
        }

        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);

        if (superAdmin == null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(superAdmin, superAdminPassword);
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                Console.WriteLine("SuperAdmin created");
            }
            else
            {
                // Log the errors if the account creation failed
                Console.WriteLine("Failed to create SuperAdmin account.");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"Error: {error.Description}");
                }
            }
        }
    }
}