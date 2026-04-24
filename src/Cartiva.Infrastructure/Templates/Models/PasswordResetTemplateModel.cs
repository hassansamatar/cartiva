namespace Cartiva.Infrastructure.Templates.Models;

public class PasswordResetTemplateModel
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public string ResetLink { get; set; } = string.Empty;
    public string ExpirationTime { get; set; } = string.Empty;
}
