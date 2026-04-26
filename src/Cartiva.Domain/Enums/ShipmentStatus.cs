using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Domain.Enums
{
    public enum ShipmentStatus
    {
        PendingApproval,
        Approved,
        Shipped,
        Delivered,
        Cancelled
    }
}
