using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SET09102_2024_5.Models
{
    /// <summary>
    /// Represents a junction entity that establishes a many-to-many relationship
    /// between Incidents and Measurements in the monitoring system.
    /// </summary>
    public class IncidentMeasurement
    {
        /// <summary>
        /// The unique identifier of the measurement associated with this incident.
        /// Serves as part of the composite primary key for this junction entity.
        /// </summary>
        public int MeasurementId { get; set; }

        /// <summary>
        /// Navigation property to the associated Measurement entity.
        /// Contains the actual measurement data linked to this incident.
        /// </summary>
        public Measurement Measurement { get; set; }

        /// <summary>
        /// The unique identifier of the incident associated with this measurement.
        /// Serves as part of the composite primary key for this junction entity.
        /// </summary>
        public int IncidentId { get; set; }

        /// <summary>
        /// Navigation property to the associated Incident entity.
        /// Contains details about the incident related to this measurement.
        /// </summary>
        public Incident Incident { get; set; }
    }
}
