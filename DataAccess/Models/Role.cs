using System;

namespace WPFGrowerApp.DataAccess.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return RoleName;
        }
    }
} 