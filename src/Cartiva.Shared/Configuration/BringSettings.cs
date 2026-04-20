namespace Cartiva.Shared.Configuration;

/// <summary>
/// Bring shipping API configuration
/// </summary>
public class BringSettings
{
    public const string SectionName = "Bring";

    public string BaseUrl { get; set; } = "https://api.bring.com/shipping/api/v1";
    public string? ApiKey { get; set; }
    public string? CustomerId { get; set; }
}
