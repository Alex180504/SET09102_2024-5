using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SET09102_2024_5.Models
{
    /// <summary>
    /// Represents a maintenance record for a sensor in the monitoring system.
    /// Contains information about maintenance activities, timing, and the personnel who performed them.
    /// </summary>
    public class Maintenance
    {
        /// <summary>
        /// The unique identifier for the maintenance record.
        /// </summary>
        public int MaintenanceId { get; set; }

        /// <summary>
        /// The date and time when the maintenance was performed.
        /// Null if the maintenance is planned but not yet completed.
        /// </summary>
        public DateTime? MaintenanceDate { get; set; }

        /// <summary>
        /// Comments provided by the maintainer about the work performed,
        /// issues identified, or follow-up actions required.
        /// </summary>
        public string MaintainerComments { get; set; }

        /// <summary>
        /// The unique identifier of the sensor that was maintained.
        /// </summary>
        public int SensorId { get; set; }

        /// <summary>
        /// Navigation property to the associated Sensor entity
        /// that was the subject of this maintenance activity.
        /// </summary>
        public Sensor Sensor { get; set; }

        /// <summary>
        /// The unique identifier of the user who performed the maintenance.
        /// </summary>
        public int MaintainerId { get; set; }

        /// <summary>
        /// Navigation property to the User entity who performed this maintenance.
        /// Contains information about the maintainer.
        /// </summary>
        public User Maintainer { get; set; }
    }
}
