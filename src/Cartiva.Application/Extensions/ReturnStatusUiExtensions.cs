using Cartiva.Domain.Enums;

namespace Cartiva.Application.Extensions;

public static class ReturnStatusUiExtensions
{
    public static string GetBadgeClass(this ReturnStatus status) => status switch
    {
        ReturnStatus.Pending => "bg-warning text-dark",
        ReturnStatus.Approved => "bg-info",
        ReturnStatus.Rejected => "bg-danger",
        ReturnStatus.Refunded => "bg-success",
        _ => "bg-secondary"
    };
}
