using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Extensions;
using Cartiva.Domain.ViewModels;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cartiva.Application.Services;

/// <summary>
/// Service for managing company operations
/// </summary>
public class CompanyService : ICompanyService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CompanyService> _logger;

    public CompanyService(ApplicationDbContext db, ILogger<CompanyService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Company>> GetAllCompaniesAsync()
    {
        return await _db.Companies
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<CompanyListVM>> GetAllCompaniesWithStatsAsync()
    {
        var companies = await _db.Companies.ToListAsync();

        var companyUsers = await _db.Users
            .Where(u => u.CompanyId != null)
            .ToListAsync();

        var orders = await _db.OrderHeaders
            .Include(o => o.ApplicationUser)
            .ToListAsync();

        return companies.Select(company =>
        {
            var companyUser = companyUsers
                .FirstOrDefault(u => u.CompanyId == company.Id);

            var allUsersForCompany = companyUsers
                .Where(u => u.CompanyId == company.Id)
                .ToList();

            var activeUsersForCompany = allUsersForCompany
                .Where(u => u.IsActive)
                .ToList();

            var companyOrders = orders
                .Where(o => o.ApplicationUser != null && o.ApplicationUser.CompanyId == company.Id)
                .ToList();

            string paymentStatus = "No Orders";

            if (companyOrders.Any())
            {
                if (companyOrders.Any(o =>
                        o.PaymentStatus == Cartiva.Domain.Enums.PaymentStatus.Deferred &&
                        o.PaymentDueDate < DateOnly.FromDateTime(DateTime.Now)))
                {
                    paymentStatus = "Overdue";
                }
                else if (companyOrders.Any(o =>
                        o.PaymentStatus == Cartiva.Domain.Enums.PaymentStatus.Deferred))
                {
                    paymentStatus = "Pending";
                }
                else if (companyOrders.All(o =>
                        o.PaymentStatus == Cartiva.Domain.Enums.PaymentStatus.Approved))
                {
                    paymentStatus = "Paid";
                }
            }

            return new CompanyListVM
            {
                Company = company,
                ContactPerson = companyUser?.Name ?? "—",
                PaymentStatus = paymentStatus,
                Users = allUsersForCompany,
                HasOrderHistory = companyOrders.Any(),
                HasActiveUsers = activeUsersForCompany.Any()
            };
        }).ToList();
    }

    public async Task<Company?> GetCompanyByIdAsync(int id)
    {
        return await _db.Companies.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CompanyOperationResult> CreateCompanyAsync(Company company)
    {
        _db.Companies.Add(company);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Company created: {CompanyName} (ID: {CompanyId})", company.Name, company.Id);
        return CompanyOperationResult.Succeeded("Company created successfully", company.Id);
    }

    public async Task<CompanyOperationResult> UpdateCompanyAsync(Company company, bool canChangeActiveStatus)
    {
        var existingCompany = await _db.Companies.FindAsync(company.Id);
        if (existingCompany == null)
        {
            return CompanyOperationResult.Failed("Company not found.");
        }

        existingCompany.Name = company.Name;
        existingCompany.StreetAddress = company.StreetAddress;
        existingCompany.City = company.City;
        existingCompany.State = company.State;
        existingCompany.PostalCode = company.PostalCode;
        existingCompany.PhoneNumber = company.PhoneNumber;

        if (canChangeActiveStatus)
        {
            existingCompany.IsActive = company.IsActive;
        }

        _db.Companies.Update(existingCompany);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Company updated: {CompanyName} (ID: {CompanyId})", company.Name, company.Id);
        return CompanyOperationResult.Succeeded("Company updated successfully", company.Id);
    }

    public async Task<CompanyOperationResult> DeleteCompanyAsync(int id)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id);
        if (company == null)
        {
            return CompanyOperationResult.Failed("Company not found.");
        }

        bool hasOrders = await HasOrdersAsync(id);
        bool hasActiveUsers = await HasActiveUsersAsync(id);

        if (hasOrders || hasActiveUsers)
        {
            // Cannot delete: mark inactive instead
            company.IsActive = false;
            _db.Companies.Update(company);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Company deactivated instead of deleted: {CompanyName} (ID: {CompanyId})", company.Name, id);
            return CompanyOperationResult.DeactivatedInstead(
                "Company has order history or active users and cannot be deleted. It has been marked inactive instead.",
                id);
        }

        // Safe to delete
        string companyName = company.Name;
        _db.Companies.Remove(company);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Company deleted: {CompanyName} (ID: {CompanyId})", companyName, id);
        return CompanyOperationResult.Succeeded("Company deleted successfully.");
    }

    public async Task<CompanyOperationResult> ToggleStatusAsync(int id)
    {
        var company = await _db.Companies.FindAsync(id);
        if (company == null)
        {
            return CompanyOperationResult.Failed("Company not found.");
        }

        company.IsActive = !company.IsActive;
        _db.Companies.Update(company);
        await _db.SaveChangesAsync();

        string status = company.IsActive ? "Active" : "Inactive";
        _logger.LogInformation("Company status toggled: {CompanyName} (ID: {CompanyId}) -> {Status}", company.Name, id, status);
        return CompanyOperationResult.Succeeded($"Company status updated to {status}.", id);
    }

    public async Task<bool> HasOrdersAsync(int companyId)
    {
        return await _db.OrderHeaders
            .Include(o => o.ApplicationUser)
            .AnyAsync(o => o.ApplicationUser != null && o.ApplicationUser.CompanyId == companyId);
    }

    public async Task<bool> HasActiveUsersAsync(int companyId)
    {
        return await _db.Users
            .AnyAsync(u => u.CompanyId == companyId && u.IsActive);
    }

    public async Task<List<ApplicationUser>> GetCompanyUsersAsync(int companyId)
    {
        return await _db.Users
            .Where(u => u.CompanyId == companyId)
            .ToListAsync();
    }
}
