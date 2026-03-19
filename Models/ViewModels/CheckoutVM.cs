using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Models.ViewModels
{
    public class CheckoutVM
    {
        public OrderHeader OrderHeader { get; set; } = new OrderHeader();

        [ValidateNever]
        public List<ShoppingCart> ShoppingCartList { get; set; } = new List<ShoppingCart>();

        [Display(Name = "Order Total")]
        [DataType(DataType.Currency)]
        public decimal OrderTotal { get; set; }

        // For displaying size information in checkout
        public string GetVariantDisplay(ProductVariant variant)
        {
            if (variant.SizeValue != null)
            {
                return $"{variant.Color} - {variant.SizeValue.DisplayText}";
            }
            return variant.Color;
        }
    }
}