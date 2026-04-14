using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Cartiva.Domain;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 30 characters.")]
    [RegularExpression(@"^(?=.*[a-zA-Z\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u00ff])[a-zA-Z0-9\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u00ff\s\-&']+$", ErrorMessage = "Category name must contain at least one letter.")]
    [DisplayName("Category Name")]
    public string Name { get; set; }

    // Optional: Default size system for this category
    [DisplayName("Default Size System")]
    public int? SizeSystemId { get; set; }

    [ForeignKey("SizeSystemId")]
    [ValidateNever]
    public SizeSystem? DefaultSizeSystem { get; set; }
}