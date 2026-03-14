using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Models.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Models
{
    public class ShoppingCart
    {
        public int Id { get; set; }

        public int Count { get; set; }

        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        public int ProductVariantId { get; set; }
        [ForeignKey("ProductVariantId")]
        [ValidateNever]
        public ProductVariant ProductVariant { get; set; }
    }
}
