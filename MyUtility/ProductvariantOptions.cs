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
        new SelectListItem { Text = "44 XS - 34 chest", Value = "44 XS - 34 chest" },
        new SelectListItem { Text = "46 S - 36 chest", Value = "46 S - 36 chest" },
        new SelectListItem { Text = "48 M - 38 chest", Value = "48 M - 38 chest" },
        new SelectListItem { Text = "50 L - 40 chest", Value = "50 L - 40 chest" },
        new SelectListItem { Text = "52 XL - 42 chest", Value = "52 XL - 42 chest" }
        
    };

        public static List<SelectListItem> KidSizes { get; } = new()
    {
        new SelectListItem { Text = "104 cm | 2 years", Value = "104 cm | 2 years" },
        new SelectListItem { Text = "68 cm | 0-6 months", Value = "68 cm | 0-6 months" },
        new SelectListItem { Text = "80 cm | 6-12 months", Value = "80 cm | 6-12 months" },
        new SelectListItem { Text = "140 cm | 9-10 years", Value = " 140 cm | 9-10 years" },
         new SelectListItem { Text = "160 cm | 12-14 years", Value = " 160 cm | 12-14 years" }
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
