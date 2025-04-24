using System;

namespace SET09102_2024_5.Models
{
    public class RolePrivilege
    {
        public int RoleId { get; set; }
        public Role Role { get; set; }
        
        public int AccessPrivilegeId { get; set; }
        public AccessPrivilege AccessPrivilege { get; set; }
    }
}