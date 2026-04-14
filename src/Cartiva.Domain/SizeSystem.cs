using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cartiva.Domain
{
    public class SizeSystem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Size System Name")]
        public string Name { get; set; }  // "Adult Regular", "Adult Suit", "Kids", "Shoe Sizes"

        [Required]
        [StringLength(20)]
        [Display(Name = "Size Type")]
        public string SizeType { get; set; }  // "Regular", "Suit", "Kid", "Shoe"

        [StringLength(200)]
        [Display(Name = "Description")]
        public string? Description { get; set; }  // "Regular sizes S-XXL"

        [StringLength(50)]
        [Display(Name = "Icon Class")]
        public string? IconClass { get; set; }  // "bi-person", "bi-person-badge", "bi-emoji-smile", "bi-box"

        [StringLength(50)]
        [Display(Name = "Alert Class")]
        public string? AlertClass { get; set; }  // "alert-info", "alert-primary", "alert-success"

        // Navigation property to SizeValues
        public virtual ICollection<SizeValue>? SizeValues { get; set; }

        // Navigation property to Categories that use this size system
        public virtual ICollection<Category>? Categories { get; set; }
    }
}