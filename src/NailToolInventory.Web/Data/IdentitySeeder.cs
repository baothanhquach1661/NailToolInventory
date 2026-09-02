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

        var roles = new[]
        {
            AdminRole,
            StaffRole
        };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(
                    new IdentityRole(roleName));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join(
                            "; ",
                            roleResult.Errors.Select(
                                error => error.Description)));
                }
            }
        }

        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];
        var fullName = configuration["SeedAdmin:FullName"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Seed admin credentials are missing.");
        }

        var admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var userResult = await userManager.CreateAsync(
                admin,
                password);

            if (!userResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        "; ",
                        userResult.Errors.Select(
                            error => error.Description)));
            }
        }
        // else if (!await userManager.CheckPasswordAsync(
        //      admin,
        //      password))
        // {
        //     var resetToken =
        //         await userManager.GeneratePasswordResetTokenAsync(admin);

        //     var passwordResult =
        //         await userManager.ResetPasswordAsync(
        //             admin,
        //             resetToken,
        //             password);

        //     if (!passwordResult.Succeeded)
        //     {
        //         throw new InvalidOperationException(
        //             string.Join(
        //                 "; ",
        //                 passwordResult.Errors.Select(
        //                     error => error.Description)));
        //     }
        // }

        if (!await userManager.IsInRoleAsync(
                admin,
                AdminRole))
        {
            var roleResult = await userManager.AddToRoleAsync(
                admin,
                AdminRole);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        "; ",
                        roleResult.Errors.Select(
                            error => error.Description)));
            }
        }
    }
}