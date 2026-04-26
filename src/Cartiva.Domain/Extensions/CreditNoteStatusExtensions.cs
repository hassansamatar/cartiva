using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class CreditNoteStatusExtensions
{
    public static string ToValue(this CreditNoteStatus status) => status.ToString();

    public static CreditNoteStatus FromValue(string value) => value switch
    {
        _ => Enum.Parse<CreditNoteStatus>(value, true)
    };
}
