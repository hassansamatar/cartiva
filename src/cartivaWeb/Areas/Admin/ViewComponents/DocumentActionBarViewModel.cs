namespace cartivaWeb.Areas.Admin.ViewComponents
{
    public class DocumentActionBarViewModel
    {
        public int DocumentId { get; set; }
        public string DocumentType { get; set; } = string.Empty; // "Invoice", "Order", "CreditNote", "ARAdjustment", "Shipment"
        public string DocumentNumber { get; set; } = string.Empty;
        public bool ShowView { get; set; } = true;
        public bool ShowPrint { get; set; } = true;
        public bool ShowEmail { get; set; } = true;
        public bool ShowResend { get; set; } = false; // For Order/Shipment
        public bool EmailSent { get; set; } = false;
        public string? EmailRecipient { get; set; }
        public string? StatusBadgeClass { get; set; }
        public string? StatusText { get; set; }
    }
}
