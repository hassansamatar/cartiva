using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Models
{
    public class ApplicationUser:IdentityUser
    {
        // Basic profile info
        // ======================
        public string Name { get; set; } = string.Empty;


        // ======================
        // Address information
        // ======================
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? State { get; set; }
       
       public string Country { get; set; } = "Norway";
        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        [ValidateNever]
       public Company? Company { get; set; }
      
        public bool IsDeleted { get; set; } = false;
    }
}
