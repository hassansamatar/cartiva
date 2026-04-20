using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.ViewModels;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cartiva.Application.Services;

/// <summary>
/// Service for managing user operations
/// </summary>
public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UserService> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        return await _db.Users
            .Include(u => u.Company)
            .ToListAsync();
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string id)
    {
        return await _userManager.FindByIdAsync(id);
    }

    public async Task<Dictionary<string, string>> GetUserRolesAsync(List<ApplicationUser> users)
    {
        var userRoles = new Dictionary<string, string>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles[user.Id] = roles.FirstOrDefault() ?? "None";
        }
        return userRoles;
    }

    public Dictionary<int, List<ApplicationUser>> GetUsersByCompany(List<ApplicationUser> users)
    {
        return users
            .Where(u => u.CompanyId != null)
            .GroupBy(u => u.CompanyId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<UserOperationResult> ActivateUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return UserOperationResult.Failed("User not found.");
        }

        user.IsActive = true;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} activated", user.Email);
            return UserOperationResult.Succeeded($"User {user.Email} has been activated.");
        }

        var errors = result.Errors.Select(e => e.Description);
        _logger.LogError("Failed to activate user {Email}: {Errors}", user.Email, string.Join(", ", errors));
        return UserOperationResult.Failed($"Failed to activate user.", errors);
    }

    public async Task<UserOperationResult> DeactivateUserAsync(string userId, string currentUsername)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return UserOperationResult.Failed("User not found.");
        }

        if (user.UserName == currentUsername)
        {
            return UserOperationResult.Failed("You cannot deactivate your own account.");
        }

        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} deactivated", user.Email);
            return UserOperationResult.Succeeded($"User {user.Email} has been deactivated.");
        }

        var errors = result.Errors.Select(e => e.Description);
        _logger.LogError("Failed to deactivate user {Email}: {Errors}", user.Email, string.Join(", ", errors));
        return UserOperationResult.Failed($"Failed to deactivate user.", errors);
    }

    public async Task<EditRolesViewModel?> GetEditRolesViewModelAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var currentRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        var companies = await _db.Companies.ToListAsync();

        return new EditRolesViewModel
        {
            UserId = user.Id,
            UserEmail = user.Email,
            UserName = user.Name ?? user.Email,
            SelectedRole = currentRoles.FirstOrDefault() ?? "None",
            AvailableRoles = allRoles,
            Companies = companies,
            CompanyId = user.CompanyId
        };
    }

    public async Task<UserOperationResult> UpdateUserRolesAsync(string userId, string? selectedRole, int? companyId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return UserOperationResult.Failed("User not found.");
        }

        // Remove existing roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Assign new role
        if (!string.IsNullOrEmpty(selectedRole) && selectedRole != "None")
        {
            await _userManager.AddToRoleAsync(user, selectedRole);

            // If role is Company, assign selected company
            if (selectedRole == SD.Role_Company)
            {
                user.CompanyId = companyId;
            }
            else
            {
                user.CompanyId = null;
            }

            await _userManager.UpdateAsync(user);
        }

        _logger.LogInformation("User {Email} role updated to {Role}", user.Email, selectedRole ?? "None");
        return UserOperationResult.Succeeded($"User {user.Email} role updated to {selectedRole ?? "None"}");
    }

    public async Task<List<string>> GetAllRolesAsync()
    {
        return await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
    }

    public async Task<List<Company>> GetCompaniesForSelectionAsync()
    {
        return await _db.Companies.ToListAsync();
    }
}
