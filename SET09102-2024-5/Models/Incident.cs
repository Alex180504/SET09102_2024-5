using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SET09102_2024_5.Models
{
    /// <summary>
    /// Represents an incident in the monitoring system that requires attention or has been resolved.
    /// Contains information about the response, resolution, and associated measurements.
    /// </summary>
    public class Incident
    {
        /// <summary>
        /// The unique identifier for the incident.
        /// </summary>
        public int IncidentId { get; set; }

        /// <summary>
        /// Comments provided by the person who responded to the incident.
        /// </summary>
        public string ResponderComments { get; set; }

        /// <summary>
        /// The date and time when the incident was resolved.
        /// Null if the incident is still open or unresolved.
        /// </summary>
        public DateTime? ResolvedDate { get; set; }

        /// <summary>
        /// The priority level of the incident (e.g., "Low", "Medium", "High", "Critical").
        /// </summary>
        public string Priority { get; set; }

        /// <summary>
        /// The unique identifier of the user who responded to this incident.
        /// Null if no responder has been assigned yet.
        /// </summary>
        public int? ResponderId { get; set; }

        /// <summary>
        /// Navigation property to the User entity who responded to this incident.
        /// </summary>
        public User Responder { get; set; }

        /// <summary>
        /// Collection of measurements associated with this incident.
        /// Represents the junction table for the many-to-many relationship between incidents and measurements.
        /// </summary>
        public ICollection<IncidentMeasurement> IncidentMeasurements { get; set; }
    }
}
