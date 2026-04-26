using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class UserRoleExtensions
{
    public static string ToValue(this UserRole role) => role.ToString();

    public static UserRole FromValue(string value) => value switch
    {
        _ => Enum.Parse<UserRole>(value, true)
    };
}
