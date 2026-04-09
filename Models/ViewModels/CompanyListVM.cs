using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class CompanyListVM
    {
        public Company Company { get; set; }

        public string? ContactPerson { get; set; }

        public string PaymentStatus { get; set; }

        public List<ApplicationUser> Users { get; set; } = new();
    }
}
