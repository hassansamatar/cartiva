using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Domain.ViewModels
{
    public class CompanyListVM
    {
        public Company Company { get; set; } = null!;

        public string? ContactPerson { get; set; }

        public string PaymentStatus { get; set; } = "No Orders";

        public List<ApplicationUser> Users { get; set; } = new();

        /// <summary>
        /// Indicates if the company has any order history (cannot be deleted if true)
        /// </summary>
        public bool HasOrderHistory { get; set; }

        /// <summary>
        /// Indicates if the company has active users assigned
        /// </summary>
        public bool HasActiveUsers { get; set; }

        /// <summary>
        /// Returns true if the company can be safely deleted
        /// </summary>
        public bool CanDelete => !HasOrderHistory && !HasActiveUsers;

        /// <summary>
        /// Gets the reason why the company cannot be deleted
        /// </summary>
        public string? DeleteBlockedReason
        {
            get
            {
                if (HasOrderHistory && HasActiveUsers)
                    return "Company has order history and active users";
                if (HasOrderHistory)
                    return "Company has order history";
                if (HasActiveUsers)
                    return "Company has active users";
                return null;
            }
        }
    }
}
