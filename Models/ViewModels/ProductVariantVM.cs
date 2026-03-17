using Microsoft.AspNetCore.Mvc.Rendering;
using MyUtility;

namespace Models.ViewModels
{

    public class ProductVariantVM
    {
        public ProductVariant Variant { get; set; } = new ProductVariant();
        public List<SelectListItem> AvailableSizes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableColors { get; set; } = new List<SelectListItem>();
    }
}
