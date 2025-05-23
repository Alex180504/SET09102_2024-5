﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SET09102_2024_5.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public bool IsProtected { get; set; }

        public ICollection<User> Users { get; set; }
        
        // Many-to-many relationship with access privileges
        public ICollection<RolePrivilege> RolePrivileges { get; set; }
    }
}
