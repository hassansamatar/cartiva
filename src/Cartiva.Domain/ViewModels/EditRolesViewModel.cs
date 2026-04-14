using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Domain.ViewModels
{
    public class EditRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public string SelectedRole { get; set; } = string.Empty;
        public int? CompanyId { get; set; }  // Nullable, only for company users
        public List<Company> Companies { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();
    }
}
