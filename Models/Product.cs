using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
    using Models.ViewModels;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

 
           public class Product
        {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
            [Required]
            [DisplayName("Product Name")]
            [MaxLength(30)]
            public string Name { get; set; }
            [Required]
            public string Brand { get; set; }
            [Required]
            

            public string? Description { get; set; }
            [Range(1, 1000)]
           
            public string? ImageUrl { get; set; }
        
        public int  CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        [ValidateNever]
        public Category Category { get; set; }
        public ICollection<ProductVariant> Variants { get; set; }

    }

}
