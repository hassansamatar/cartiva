using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Domain.ViewModels
{
    public class ProductVM
    {
        public Product Product { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> CategoryList { get; set; }
        // Add this to handle variants in the product form
        [ValidateNever]
        public List<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

        // Optional: For tracking deleted variants during edit
        [ValidateNever]
        public List<int> DeletedVariantIds { get; set; } = new List<int>();
    }

}
