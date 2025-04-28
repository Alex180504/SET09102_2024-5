using System.ComponentModel.DataAnnotations.Schema;

namespace SET09102_2024_5.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }

        public int? RoleId { get; set; }
        public Role Role { get; set; }

        public ICollection<Maintenance> Maintenances { get; set; }
        public ICollection<Incident> RespondedIncidents { get; set; }

        [NotMapped]
        public string DisplayName { get; set; }
    }
}
