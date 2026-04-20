using Cartiva.Domain;
using Cartiva.Domain.ViewModels;

namespace Cartiva.Application.Abstractions;

/// <summary>
/// Service for managing user operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get all users with company information
    /// </summary>
    Task<List<ApplicationUser>> GetAllUsersAsync();

    /// <summary>
    /// Get a user by ID
    /// </summary>
    Task<ApplicationUser?> GetUserByIdAsync(string id);

    /// <summary>
    /// Get user roles mapping
    /// </summary>
    Task<Dictionary<string, string>> GetUserRolesAsync(List<ApplicationUser> users);

    /// <summary>
    /// Get users grouped by company
    /// </summary>
    Dictionary<int, List<ApplicationUser>> GetUsersByCompany(List<ApplicationUser> users);

    /// <summary>
    /// Activate a user
    /// </summary>
    Task<UserOperationResult> ActivateUserAsync(string userId);

    /// <summary>
    /// Deactivate a user
    /// </summary>
    Task<UserOperationResult> DeactivateUserAsync(string userId, string currentUsername);

    /// <summary>
    /// Get role editing data for a user
    /// </summary>
    Task<EditRolesViewModel?> GetEditRolesViewModelAsync(string userId);

    /// <summary>
    /// Update user roles and company assignment
    /// </summary>
    Task<UserOperationResult> UpdateUserRolesAsync(string userId, string? selectedRole, int? companyId);

    /// <summary>
    /// Get all available roles
    /// </summary>
    Task<List<string>> GetAllRolesAsync();

    /// <summary>
    /// Get all companies for selection
    /// </summary>
    Task<List<Company>> GetCompaniesForSelectionAsync();
}

/// <summary>
/// Result of a user operation
/// </summary>
public class UserOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static UserOperationResult Succeeded(string message)
        => new() { Success = true, Message = message };

    public static UserOperationResult Failed(string message)
        => new() { Success = false, Message = message };

    public static UserOperationResult Failed(string message, IEnumerable<string> errors)
        => new() { Success = false, Message = message, Errors = errors.ToList() };
}
