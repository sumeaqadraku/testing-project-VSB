using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Web.Data.Seed;

public static class DbInitializer
{
    public const string ManagerRole = "Manager";
    public const string MechanicRole = "Mechanic";
    public const string ClientRole = "Client";

    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

      
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }

        await SeedRolesAsync(roleManager);

        await SeedManagerUserAsync(userManager);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { ManagerRole, MechanicRole, ClientRole };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedManagerUserAsync(UserManager<ApplicationUser> userManager)
    {
        const string managerEmail = "manager@vehicleservice.com";
        const string managerPassword = "Manager@123";

        var manager = await userManager.FindByEmailAsync(managerEmail);
        if (manager != null)
            return;

        manager = new ApplicationUser
        {
            UserName = managerEmail,
            Email = managerEmail,
            NormalizedUserName = managerEmail.ToUpper(),
            NormalizedEmail = managerEmail.ToUpper(),
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Manager",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(manager, managerPassword);
        if (!result.Succeeded)
        {
            throw new Exception(
                $"Failed to create Manager user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(manager, ManagerRole);
    }
}
