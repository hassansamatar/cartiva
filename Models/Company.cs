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

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 100 characters.")]
        [RegularExpression(@"^(?=.*[a-zA-Z\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u00ff])[a-zA-Z0-9\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u00ff\s\-&'.]+$", ErrorMessage = "Company name must contain at least one letter.")]
        [Display(Name = "Company Name")]
        public string? Name { get; set; }

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

        [RegularExpression(@"^\+?\d[\d\s\-]{6,18}\d$", ErrorMessage = "Please enter a valid phone number (e.g. +47 12345678).")]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
