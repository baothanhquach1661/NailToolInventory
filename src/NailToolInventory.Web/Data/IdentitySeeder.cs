using Microsoft.AspNetCore.Identity;
using NailToolInventory.Models;

namespace NailToolInventory.Data;

public static class IdentitySeeder
{
    public const string AdminRole = "Admin";
    public const string StaffRole = "Staff";

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureRoleAsync(roleManager, AdminRole);
        await EnsureRoleAsync(roleManager, StaffRole);

        await EnsureUserAsync(
            userManager,
            configuration,
            "SeedAdmin",
            AdminRole);

        await EnsureUserAsync(
            userManager,
            configuration,
            "SeedStaff",
            StaffRole);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole> roleManager,
        string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await roleManager.CreateAsync(
            new IdentityRole(roleName));

        ThrowIfFailed(result);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        string configurationSection,
        string roleName)
    {
        var email =
            configuration[$"{configurationSection}:Email"];

        var password =
            configuration[$"{configurationSection}:Password"];

        var fullName =
            configuration[$"{configurationSection}:FullName"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"{configurationSection} credentials are missing.");
        }

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(
                user,
                password);

            ThrowIfFailed(createResult);
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            var roleResult = await userManager.AddToRoleAsync(
                user,
                roleName);

            ThrowIfFailed(roleResult);
        }
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new InvalidOperationException(
            string.Join(
                "; ",
                result.Errors.Select(
                    error => error.Description)));
    }
}