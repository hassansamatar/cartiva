using System;
using System.Collections.Generic;
using System.Text;
namespace Cartiva.Application.Abstractions;

public interface ICompanyShipmentApprovalService
{
    Task<int> ApprovePendingCompanyShipmentsAsync(CancellationToken ct);
}