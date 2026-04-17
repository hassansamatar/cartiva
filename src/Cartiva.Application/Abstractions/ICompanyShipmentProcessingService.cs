using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Application.Abstractions;

public interface ICompanyShipmentProcessingService
{
    Task<int> ProcessApprovedShipmentsAsync(CancellationToken ct);
}