using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NailToolInventory.Data;
using NailToolInventory.Models;
using NailToolInventory.ViewModels;

namespace NailToolInventory.Controllers;

[Authorize(Roles = IdentitySeeder.AdminRole)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;


    public UsersController(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }


    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User);

        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ToListAsync();

        var model = new UserListViewModel();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            model.Users.Add(new UserListItemViewModel
            {
                Id = user.Id,

                Email =
                    user.Email ??
                    user.UserName ??
                    "(No email)",

                FullName =
                    string.IsNullOrWhiteSpace(user.FullName)
                        ? "—"
                        : user.FullName,

                Role = roles.Count == 0
                    ? "No Role"
                    : string.Join(", ", roles),

                IsLockedOut =
                    user.LockoutEnd.HasValue &&
                    user.LockoutEnd.Value >
                    DateTimeOffset.UtcNow,

                IsCurrentUser = user.Id == currentUserId
            });
        }

        return View(model);
    }


    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateStaffUserViewModel());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateStaffUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email
            .Trim()
            .ToLowerInvariant();

        var existingUser = await _userManager
            .FindByEmailAsync(normalizedEmail);

        if (existingUser is not null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "An account with this email already exists.");

            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FullName = model.FullName.Trim(),
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var createResult = await _userManager.CreateAsync(
            user,
            model.Password);

        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            IdentitySeeder.StaffRole);

        if (!roleResult.Succeeded)
        {
            // Không giữ tài khoản nếu việc gán role thất bại.
            await _userManager.DeleteAsync(user);

            AddIdentityErrors(roleResult);
            return View(model);
        }

        TempData["SuccessMessage"] =
            $"Staff account {normalizedEmail} was created successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(
    string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        var isStaff = await _userManager.IsInRoleAsync(
            user,
            IdentitySeeder.StaffRole);

        var isAdmin = await _userManager.IsInRoleAsync(
            user,
            IdentitySeeder.AdminRole);

        if (!isStaff || isAdmin)
        {
            TempData["ErrorMessage"] =
                "Password can only be reset for Staff accounts.";

            return RedirectToAction(nameof(Index));
        }

        var model = new ResetStaffPasswordViewModel
        {
            UserId = user.Id,

            FullName =
                string.IsNullOrWhiteSpace(user.FullName)
                    ? "—"
                    : user.FullName,

            Email =
                user.Email ??
                user.UserName ??
                "(No email)"
        };

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetStaffPasswordViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.UserId))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(
            model.UserId);

        if (user is null)
        {
            return NotFound();
        }

        // Luôn lấy lại thông tin từ database.
        model.FullName =
            string.IsNullOrWhiteSpace(user.FullName)
                ? "—"
                : user.FullName;

        model.Email =
            user.Email ??
            user.UserName ??
            "(No email)";

        var isStaff = await _userManager.IsInRoleAsync(
            user,
            IdentitySeeder.StaffRole);

        var isAdmin = await _userManager.IsInRoleAsync(
            user,
            IdentitySeeder.AdminRole);

        if (!isStaff || isAdmin)
        {
            TempData["ErrorMessage"] =
                "Password can only be reset for Staff accounts.";

            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resetToken =
            await _userManager.GeneratePasswordResetTokenAsync(
                user);

        var result = await _userManager.ResetPasswordAsync(
            user,
            resetToken,
            model.NewPassword);

        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        TempData["SuccessMessage"] =
            $"Password for {model.Email} was reset successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(
    string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);

        if (user.Id == currentUserId)
        {
            TempData["ErrorMessage"] =
                "You cannot disable your own account.";

            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.IsInRoleAsync(
                user,
                IdentitySeeder.AdminRole))
        {
            TempData["ErrorMessage"] =
                "Administrator accounts cannot be disabled here.";

            return RedirectToAction(nameof(Index));
        }

        if (!await _userManager.IsInRoleAsync(
                user,
                IdentitySeeder.StaffRole))
        {
            TempData["ErrorMessage"] =
                "Only Staff accounts can be enabled or disabled.";

            return RedirectToAction(nameof(Index));
        }

        var isCurrentlyDisabled =
            user.LockoutEnd.HasValue &&
            user.LockoutEnd.Value > DateTimeOffset.UtcNow;

        if (!isCurrentlyDisabled && !user.LockoutEnabled)
        {
            var lockoutResult =
                await _userManager.SetLockoutEnabledAsync(
                    user,
                    true);

            if (!lockoutResult.Succeeded)
            {
                TempData["ErrorMessage"] =
                    string.Join(
                        "; ",
                        lockoutResult.Errors.Select(
                            error => error.Description));

                return RedirectToAction(nameof(Index));
            }
        }

        DateTimeOffset? newLockoutEnd =
            isCurrentlyDisabled
                ? null
                : DateTimeOffset.MaxValue;

        var result =
            await _userManager.SetLockoutEndDateAsync(
                user,
                newLockoutEnd);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] =
                string.Join(
                    "; ",
                    result.Errors.Select(
                        error => error.Description));

            return RedirectToAction(nameof(Index));
        }

        if (!isCurrentlyDisabled)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        var email =
            user.Email ??
            user.UserName ??
            "The staff account";

        TempData["SuccessMessage"] = isCurrentlyDisabled
            ? $"{email} was enabled successfully."
            : $"{email} was disabled successfully.";

        return RedirectToAction(nameof(Index));
    }

    private void AddIdentityErrors(
        IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }
    }
}