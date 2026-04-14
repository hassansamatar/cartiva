using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cartiva.Domain
{
    public class SizeValue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [DisplayName("Size System")]
        public int SizeSystemId { get; set; }

        [Required]
        [StringLength(50)]
        [DisplayName("Size Value")]
        public string Value { get; set; }  // "M", "48", "104"

        [Required]
        [StringLength(50)]
        [DisplayName("Display Text")]
        public string DisplayText { get; set; }  // "M", "48", "104 cm"

        [StringLength(200)]
        [DisplayName("Description")]
        public string? Description { get; set; }

        [DisplayName("Sort Order")]
        public int SortOrder { get; set; }  // For consistent ordering in dropdowns

        // Navigation properties
        [ForeignKey("SizeSystemId")]
        [ValidateNever]
        public SizeSystem SizeSystem { get; set; }

        [ValidateNever]
        public ICollection<ProductVariant> ProductVariants { get; set; }
    }
}