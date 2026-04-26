using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class PaymentStatusExtensions
{
    public static string ToValue(this PaymentStatus status) => status.ToString();

    public static PaymentStatus FromValue(string value) => value switch
    {
        _ => Enum.Parse<PaymentStatus>(value, true)
    };
}
