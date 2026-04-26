using Cartiva.Domain.Enums;

namespace Cartiva.Application.Extensions;

public static class CreditNoteStatusUiExtensions
{
    public static string GetBadgeClass(this CreditNoteStatus status) => status switch
    {
        CreditNoteStatus.Draft => "bg-secondary",
        CreditNoteStatus.Issued => "bg-info",
        CreditNoteStatus.Booked => "bg-success",
        CreditNoteStatus.Cancelled => "bg-danger",
        _ => "bg-secondary"
    };

    public static string GetIcon(this CreditNoteStatus status) => status switch
    {
        CreditNoteStatus.Draft => "bi-file-earmark",
        CreditNoteStatus.Issued => "bi-file-earmark-minus",
        CreditNoteStatus.Booked => "bi-journal-check",
        CreditNoteStatus.Cancelled => "bi-x-circle",
        _ => "bi-file-earmark"
    };
}
