using Cartiva.Domain.Enums;

namespace Cartiva.Application.Extensions;

public static class SizeTypeUiExtensions
{
    public static string GetIcon(this SizeType sizeType) => sizeType switch
    {
        SizeType.Regular => "bi-person",
        SizeType.Suit => "bi-person-badge",
        SizeType.Kid => "bi-emoji-smile",
        SizeType.Shoe => "bi-box",
        _ => "bi-tag"
    };

    public static string GetAlertClass(this SizeType sizeType) => sizeType switch
    {
        SizeType.Regular => "alert-info",
        SizeType.Suit => "alert-primary",
        SizeType.Kid => "alert-success",
        SizeType.Shoe => "alert-warning",
        _ => "alert-secondary"
    };
}
