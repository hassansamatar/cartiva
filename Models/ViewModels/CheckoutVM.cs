using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class CheckoutVM
    {
        public OrderHeader OrderHeader { get; set; }
        public List<ShoppingCart> ShoppingCartList { get; set; }
        public decimal OrderTotal { get; set; }
        // Optional fields: ShippingMethod, PaymentMethod, Discounts, etc.
    }
}
