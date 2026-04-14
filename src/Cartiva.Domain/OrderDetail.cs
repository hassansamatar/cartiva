using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Cartiva.Domain
{
    public class OrderDetail
    {
        public int Id { get; set; }

        [Required]
        public int OrderHeaderId { get; set; }

        [ForeignKey("OrderHeaderId")]
        [ValidateNever]
        public OrderHeader? OrderHeader { get; set; }

        [Required]
        public int ProductVariantId { get; set; }

        [ForeignKey("ProductVariantId")]
        [ValidateNever]
        public ProductVariant ProductVariant { get; set; }

        [Required]
        public int Count { get; set; }

        public decimal Price { get; set; }
    }
}
