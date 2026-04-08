using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Models
{
    public class Promotion
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Promotion name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Promotion name must be between 2 and 100 characters.")]
        [RegularExpression(@"^(?=.*[a-zA-Z\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u00ff])[a-zA-Z0-9\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u00ff\s\-&'!%]+$", ErrorMessage = "Promotion name must contain at least one letter.")]
        [Display(Name = "Promotion Name")]
        public string Name { get; set; }

        [StringLength(300)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Buy quantity is required.")]
        [Range(2, 10, ErrorMessage = "Buy quantity must be between 2 and 10.")]
        [Display(Name = "Buy Quantity")]
        public int BuyQuantity { get; set; }

        [Required(ErrorMessage = "Get quantity is required.")]
        [Range(1, 10, ErrorMessage = "Get quantity must be between 1 and 10.")]
        [Display(Name = "Get Quantity")]
        public int GetQuantity { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        [ValidateNever]
        public Category Category { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Computed
        [NotMapped]
        public bool IsCurrentlyActive => IsActive && DateTime.Now >= StartDate && DateTime.Now <= EndDate;

        [NotMapped]
        public string DisplayText => $"Buy {BuyQuantity} Get {GetQuantity} Free";
    }
}
