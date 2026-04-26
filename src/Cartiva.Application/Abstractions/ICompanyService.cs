using Cartiva.Domain;
using Cartiva.Domain.ViewModels;

namespace Cartiva.Application.Abstractions;

/// <summary>
/// Service for managing company operations
/// </summary>
public interface ICompanyService
{
    /// <summary>
    /// Get all companies with user and order statistics
    /// </summary>
    Task<List<CompanyListVM>> GetAllCompaniesWithStatsAsync();

    /// <summary>
    /// Get all companies (simple list without stats)
    /// </summary>
    Task<List<Company>> GetAllCompaniesAsync();

    /// <summary>
    /// Get a company by ID
    /// </summary>
    Task<Company?> GetCompanyByIdAsync(int id);

    /// <summary>
    /// Create a new company
    /// </summary>
    Task<CompanyOperationResult> CreateCompanyAsync(Company company);

    /// <summary>
    /// Update an existing company
    /// </summary>
    Task<CompanyOperationResult> UpdateCompanyAsync(Company company, bool canChangeActiveStatus);

    /// <summary>
    /// Delete a company (or deactivate if has orders/users)
    /// </summary>
    Task<CompanyOperationResult> DeleteCompanyAsync(int id);

    /// <summary>
    /// Toggle company active status
    /// </summary>
    Task<CompanyOperationResult> ToggleStatusAsync(int id);

    /// <summary>
    /// Check if company has orders
    /// </summary>
    Task<bool> HasOrdersAsync(int companyId);

    /// <summary>
    /// Check if company has active users
    /// </summary>
    Task<bool> HasActiveUsersAsync(int companyId);

    /// <summary>
    /// Get users for a company
    /// </summary>
    Task<List<ApplicationUser>> GetCompanyUsersAsync(int companyId);
}

/// <summary>
/// Result of a company operation
/// </summary>
public class CompanyOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public bool WasDeactivatedInstead { get; set; }

    public static CompanyOperationResult Succeeded(string message, int? entityId = null)
        => new() { Success = true, Message = message, EntityId = entityId };

    public static CompanyOperationResult Failed(string message)
        => new() { Success = false, Message = message };

    public static CompanyOperationResult DeactivatedInstead(string message, int entityId)
        => new() { Success = false, Message = message, EntityId = entityId, WasDeactivatedInstead = true };
}
