using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Domain.Enums
{
    public enum InvoiceStatus
    {
        Draft,
        Issued,
        Sent,
        Paid,
        PartiallyPaid,
        Overdue,
        Cancelled
    }
}
