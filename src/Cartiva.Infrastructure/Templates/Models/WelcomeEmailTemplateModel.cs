namespace Cartiva.Infrastructure.Templates.Models;

public class WelcomeEmailTemplateModel
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public string? VerificationLink { get; set; }
}
