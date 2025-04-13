using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SET09102_2024_5.Models
{
    public class Incident
    {
        public int IncidentId { get; set; }
        public string ResponderComments { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public string Priority { get; set; }

        public int? ResponderId { get; set; }
        public User Responder { get; set; }

        public ICollection<IncidentMeasurement> IncidentMeasurements { get; set; }
    }
}
