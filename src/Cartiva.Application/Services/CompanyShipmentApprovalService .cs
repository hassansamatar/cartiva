using Cartiva.Application.Abstractions;
using Cartiva.Domain.Extensions;
using Cartiva.Domain.Enums;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cartiva.Application.Services;

public class CompanyShipmentApprovalService : ICompanyShipmentApprovalService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CompanyShipmentApprovalService> _logger;

    public CompanyShipmentApprovalService(ApplicationDbContext db, ILogger<CompanyShipmentApprovalService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> ApprovePendingCompanyShipmentsAsync(CancellationToken ct)
    {
        var shipments = await _db.Shipments
            .Include(s => s.OrderHeader)
                .ThenInclude(o => o.ApplicationUser)
                    .ThenInclude(u => u.Company)
            .Where(s => s.ShipmentStatus == ShipmentStatus.PendingApproval &&
                        s.OrderHeader.OrderStatus == OrderStatus.AwaitingShipmentApproval &&
                        s.OrderHeader.ApplicationUser.CompanyId != null &&
                        s.OrderHeader.ApplicationUser.Company != null &&
                        s.OrderHeader.ApplicationUser.Company.IsActive)
            .Take(50)
            .ToListAsync(ct);

        if (!shipments.Any())
        {
            _logger.LogInformation("No pending shipments for active companies.");
            return 0;
        }

        foreach (var shipment in shipments)
        {
            shipment.ShipmentStatus = ShipmentStatus.Approved;
            _logger.LogInformation("Auto-approved shipment for OrderHeaderId {OrderId} (active company)", shipment.OrderHeaderId);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Approved {Count} shipments for active companies.", shipments.Count);
        return shipments.Count;
    }
}