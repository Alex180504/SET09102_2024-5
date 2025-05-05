using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SET09102_2024_5.Models
{
    /// <summary>
    /// Represents a single data point captured by a sensor in the monitoring system.
    /// Contains the measured value, timestamp, and references to the associated sensor and physical quantity.
    /// </summary>
    public class Measurement
    {
        /// <summary>
        /// The unique identifier for the measurement record.
        /// </summary>
        [Key]
        public int MeasurementId { get; set; }

        /// <summary>
        /// The date and time when this measurement was recorded.
        /// Null if the timestamp was not captured or is unknown.
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// The actual value measured by the sensor.
        /// Null if no reading was obtained or the reading was invalid.
        /// </summary>
        public float? Value { get; set; }

        /// <summary>
        /// The unique identifier of the sensor that took this measurement.
        /// Required field that cannot be null.
        /// </summary>
        [Required]
        public int SensorId { get; set; }

        /// <summary>
        /// Navigation property to the associated Sensor entity.
        /// Contains information about the sensor that recorded this measurement.
        /// </summary>
        [ForeignKey("SensorId")]
        public Sensor Sensor { get; set; }

        /// <summary>
        /// The unique identifier of the physical quantity that was measured.
        /// Required field that cannot be null.
        /// </summary>
        [Required]
        public int PhysicalQuantityId { get; set; }

        /// <summary>
        /// Navigation property to the associated PhysicalQuantity entity.
        /// Contains information about the type of quantity that was measured and its unit.
        /// </summary>
        [ForeignKey("PhysicalQuantityId")]
        public PhysicalQuantity PhysicalQuantity { get; set; }

        /// <summary>
        /// Collection of incident associations for this measurement.
        /// Represents a many-to-many relationship between measurements and incidents.
        /// Initialized as an empty list to prevent null reference exceptions.
        /// </summary>
        public ICollection<IncidentMeasurement> IncidentMeasurements { get; set; } = new List<IncidentMeasurement>();
    }
}
