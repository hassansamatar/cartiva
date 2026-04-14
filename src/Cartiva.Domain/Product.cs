using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cartiva.Domain
{
    public class Product
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [DisplayName("Product Name")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 30 characters.")]
        [RegularExpression(@"^(?=.*[a-zA-Z\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u00ff])[a-zA-Z0-9\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u00ff\s\-&']+$", ErrorMessage = "Product name must contain at least one letter.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Brand is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Brand must be between 2 and 50 characters.")]
        [RegularExpression(@"^(?=.*[a-zA-Z\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u00ff])[a-zA-Z0-9\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u00ff\s\-&'.]+$", ErrorMessage = "Brand must contain at least one letter.")]
        public string Brand { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        [ValidateNever]
        public Category Category { get; set; }

        // Navigation property - a product can have many variants
        [ValidateNever]
        public ICollection<ProductVariant> Variants { get; set; }
    }
}