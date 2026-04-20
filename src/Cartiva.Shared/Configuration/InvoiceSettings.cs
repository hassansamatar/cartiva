namespace Cartiva.Shared.Configuration;

/// <summary>
/// Invoice generation settings
/// </summary>
public class InvoiceSettings
{
    public const string SectionName = "Invoice";

    public string BankAccount { get; set; } = string.Empty;
}
