using System;
using System.Collections.Generic;

namespace SET09102_2024_5.Models
{
    public class AccessPrivilege
    {
        public int AccessPrivilegeId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ModuleName { get; set; }  // Which part of the system this privilege applies to
        
        // Many-to-many relationship with roles
        public ICollection<RolePrivilege> RolePrivileges { get; set; }
    }
}