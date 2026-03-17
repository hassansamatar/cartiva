using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;

    namespace MyUtility
    {
     public static class ProductVariantOptions
    {
        public static List<SelectListItem> AdultSizes { get; } = new()
    {
        new SelectListItem { Text = "S", Value = "S" },
        new SelectListItem { Text = "M", Value = "M" },
        new SelectListItem { Text = "L", Value = "L" },
        new SelectListItem { Text = "XL", Value = "XL" },
        new SelectListItem { Text = "XXL", Value = "XXL" }
    };

       
        public static List<SelectListItem> AdultSuitSizes { get; } = new()
    {
        new SelectListItem { Text = "36", Value = "36" },
        new SelectListItem { Text = "38", Value = "38" },
        new SelectListItem { Text = "40", Value = "40" },
        new SelectListItem { Text = "42", Value = "42" },
        new SelectListItem { Text = "44", Value = "44" },
        new SelectListItem { Text = "46", Value = "46" }
    };

        public static List<SelectListItem> KidSizes { get; } = new()
    {
        new SelectListItem { Text = "2 Years", Value = "2" },
        new SelectListItem { Text = "3 Years", Value = "3" },
        new SelectListItem { Text = "4 Years", Value = "4" },
        new SelectListItem { Text = "5 Years", Value = "5" }
    };

        public static List<SelectListItem> Colors { get; } = new()
    {
        new SelectListItem { Text = "Red", Value = "Red" },
        new SelectListItem { Text = "Blue", Value = "Blue" },
        new SelectListItem { Text = "Green", Value = "Green" },
        new SelectListItem { Text = "Black", Value = "Black" },
        new SelectListItem { Text = "White", Value = "White" }
    };
    }
}
