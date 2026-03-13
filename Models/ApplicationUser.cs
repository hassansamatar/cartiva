using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class ApplicationUser:IdentityUser
    {
        // Basic profile info
        // ======================
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // ======================
        // Address information
        // ======================
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }
}
