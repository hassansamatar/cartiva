using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class SizeTypeExtensions
{
    public static string ToValue(this SizeType sizeType) => sizeType.ToString();

    public static SizeType FromValue(string value) => value switch
    {
        _ => Enum.Parse<SizeType>(value, true)
    };
}
