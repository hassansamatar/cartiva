using Cartiva.Domain.Enums;

namespace Cartiva.Application.Extensions;

public static class PaymentStatusUiExtensions
{
    public static string GetBadgeClass(this PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "bg-warning text-dark",
        PaymentStatus.Approved => "bg-success",
        PaymentStatus.Deferred => "bg-info",
        PaymentStatus.Rejected => "bg-danger",
        PaymentStatus.Refunded => "bg-secondary",
        PaymentStatus.Paid => "bg-success",
        _ => "bg-secondary"
    };

    public static string GetIcon(this PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "bi-clock",
        PaymentStatus.Approved => "bi-check-circle",
        PaymentStatus.Deferred => "bi-building",
        PaymentStatus.Rejected => "bi-x-circle",
        PaymentStatus.Refunded => "bi-arrow-return-left",
        PaymentStatus.Paid => "bi-check-circle-fill",
        _ => "bi-credit-card"
    };
}
