using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class EditRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string CurrentRole { get; set; }
        public List<string> AvailableRoles { get; set; }
        public string SelectedRole { get; set; }
    }
}
