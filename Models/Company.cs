using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Models
{

    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Company Name")]
        public string? Name { get; set; }

        [Display(Name = "Street Address")]
        public string? StreetAddress { get; set; }

        public string? City { get; set; }

        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        public string? State { get; set; }
          // In Models/ApplicationUser.cs
public string Country { get; set; } = "Norway";
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;



    }
}
