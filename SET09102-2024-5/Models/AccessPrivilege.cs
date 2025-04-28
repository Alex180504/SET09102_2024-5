using System;
using System.Collections.Generic;

namespace SET09102_2024_5.Models
{
    /// <summary>
    /// Represents a system access privilege that can be assigned to roles
    /// </summary>
    public class AccessPrivilege
    {
        public int AccessPrivilegeId { get; set; }
        
        /// <summary>
        /// The name of the privilege (e.g., "user.create", "role.edit")
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Human-readable description of what this privilege allows
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// The module/section this privilege belongs to (e.g., "User Management", "Role Management")
        /// </summary>
        public string ModuleName { get; set; }
        
        /// <summary>
        /// Many-to-many relationship with roles
        /// </summary>
        public ICollection<RolePrivilege> RolePrivileges { get; set; }
    }
}