namespace Cartiva.Shared.Configuration;

/// <summary>
/// Email service configuration settings
/// </summary>
public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}
