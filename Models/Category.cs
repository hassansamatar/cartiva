using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    [DisplayName("Category Name")]
    public string Name { get; set; }

    // Optional: Default size system for this category
    [DisplayName("Default Size System")]
    public int? SizeSystemId { get; set; }

    [ForeignKey("SizeSystemId")]
    [ValidateNever]
    public SizeSystem? DefaultSizeSystem { get; set; }
}