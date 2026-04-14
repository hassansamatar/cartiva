using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Cartiva.Domain
{
    public class ApplicationUser:IdentityUser
    {
        // Basic profile info
        // ======================
        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
        [RegularExpression(@"^[a-zA-ZÀ-ÖØ-öø-ÿ\s\-']+$", ErrorMessage = "Name can only contain letters, spaces, hyphens and apostrophes.")]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        // ======================
        // Address information
        // ======================
        [StringLength(100)]
        [Display(Name = "Street Address")]
        public string? StreetAddress { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(10)]
        [RegularExpression(@"^\d{4,10}$", ErrorMessage = "Postal code must be 4-10 digits.")]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        [StringLength(50)]
        [Display(Name = "State / Region")]
        public string? State { get; set; }

        [StringLength(50)]
        public string Country { get; set; } = "Norway";
        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        [ValidateNever]
       public Company? Company { get; set; }
      
        public bool IsActive { get; set; } = true;
    }
}
