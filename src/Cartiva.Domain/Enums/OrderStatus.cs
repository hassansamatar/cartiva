using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Domain.Enums
{
    public enum OrderStatus
    {
        Pending,
        Approved,
        Processing,
        AwaitingShipmentApproval,
        Shipped,
        OutForDelivery,
        Delivered,
        Cancelled,
        Refunded,
        Completed
    }
}
