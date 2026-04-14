using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cartiva.Domain.ViewModels
{
    public class ProductVariantVM
    {
        public ProductVariant Variant { get; set; }

        [ValidateNever]
        public List<SelectListItem> AvailableColors { get; set; }

        [ValidateNever]
        public List<SelectListItem> AvailableSizes { get; set; }

        [ValidateNever]
        public string ProductName { get; set; }

        [ValidateNever]
        public SizeSystem SizeSystem { get; set; }

        // Helper properties for the view
        public string SizeDisplayInfo => SizeSystem?.Description ?? "Select a size system";

        public bool HasSizeSystem => SizeSystem != null;

        // For displaying the selected size details
        public string SelectedSizeDisplay
        {
            get
            {
                if (Variant?.SizeValue != null)
                {
                    return $"{Variant.SizeValue.DisplayText} ({Variant.SizeValue.Description})";
                }
                return string.Empty;
            }
        }

        // For grouping sizes by system in dropdowns (if needed)
        [ValidateNever]
        public Dictionary<string, List<SelectListItem>> GroupedSizes { get; set; }
    }
}