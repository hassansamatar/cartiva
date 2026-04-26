using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Domain.Enums
{
    public enum PaymentStatus
    {
        Pending,
        Approved,
        Deferred,
        Rejected,
        Refunded,
        Paid
    }
}
