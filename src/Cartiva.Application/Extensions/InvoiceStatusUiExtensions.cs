using Cartiva.Domain.Enums;

namespace Cartiva.Application.Extensions;

public static class InvoiceStatusUiExtensions
{
    public static string GetBadgeClass(this InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "bg-secondary",
        InvoiceStatus.Issued => "bg-info",
        InvoiceStatus.Sent => "bg-primary",
        InvoiceStatus.Paid => "bg-success",
        InvoiceStatus.PartiallyPaid => "bg-warning text-dark",
        InvoiceStatus.Overdue => "bg-danger",
        InvoiceStatus.Cancelled => "bg-dark",
        _ => "bg-secondary"
    };

    public static string GetIcon(this InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "bi-file-earmark",
        InvoiceStatus.Issued => "bi-file-earmark-check",
        InvoiceStatus.Sent => "bi-send",
        InvoiceStatus.Paid => "bi-check-circle-fill",
        InvoiceStatus.PartiallyPaid => "bi-pie-chart",
        InvoiceStatus.Overdue => "bi-exclamation-triangle",
        InvoiceStatus.Cancelled => "bi-x-circle",
        _ => "bi-file-earmark"
    };
}
