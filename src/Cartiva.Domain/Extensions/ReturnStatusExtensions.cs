using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class ReturnStatusExtensions
{
    public static string ToValue(this ReturnStatus status) => status.ToString();

    public static ReturnStatus FromValue(string value) => value switch
    {
        _ => Enum.Parse<ReturnStatus>(value, true)
    };
}
