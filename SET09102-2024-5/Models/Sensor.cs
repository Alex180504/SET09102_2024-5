using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SET09102_2024_5.Models
{
    /// <summary>
    /// Represents a physical sensor device in the monitoring system.
    /// Contains information about the sensor's type, status, and associated data.
    /// Acts as the central entity connecting measurements, configuration, and maintenance records.
    /// </summary>
    public class Sensor
    {
        /// <summary>
        /// The unique identifier for the sensor.
        /// </summary>
        [Key]
        public int SensorId { get; set; }

        /// <summary>
        /// The type or model of the sensor device.
        /// For example: "Temperature Sensor", "Pressure Transducer", "Humidity Detector".
        /// Maximum length: 100 characters.
        /// </summary>
        [StringLength(100)]
        public string SensorType { get; set; }

        /// <summary>
        /// The current operational status of the sensor.
        /// Typically values like "Active", "Inactive", "Maintenance", "Fault", etc.
        /// Maximum length: 50 characters.
        /// </summary>
        [StringLength(50)]
        public string Status { get; set; }

        /// <summary>
        /// The date and time when this sensor was deployed to its location.
        /// Null if the sensor has not been deployed yet or the deployment date is unknown.
        /// </summary>
        public DateTime? DeploymentDate { get; set; }

        /// <summary>
        /// The unique identifier of the measurand (physical quantity) that this sensor measures.
        /// Required field that cannot be null.
        /// </summary>
        [Required]
        public int MeasurandId { get; set; }

        /// <summary>
        /// Navigation property to the associated Measurand entity.
        /// Contains information about the physical quantity being measured by this sensor.
        /// </summary>
        [ForeignKey("MeasurandId")]
        public Measurand Measurand { get; set; }

        /// <summary>
        /// Navigation property to the sensor's configuration settings.
        /// Contains location, orientation, and operational parameters.
        /// One-to-one relationship between Sensor and Configuration.
        /// </summary>
        public Configuration Configuration { get; set; }

        /// <summary>
        /// Navigation property to the sensor's firmware information.
        /// Contains firmware version and update history.
        /// One-to-one relationship between Sensor and SensorFirmware.
        /// </summary>
        public SensorFirmware Firmware { get; set; }

        /// <summary>
        /// Collection of measurements recorded by this sensor.
        /// One-to-many relationship from Sensor to Measurement.
        /// </summary>
        public ICollection<Measurement> Measurements { get; set; }

        /// <summary>
        /// Collection of maintenance records for this sensor.
        /// One-to-many relationship from Sensor to Maintenance.
        /// </summary>
        public ICollection<Maintenance> Maintenances { get; set; }

        /// <summary>
        /// A human-readable display name for the sensor, typically combining type and ID.
        /// Not stored in the database.
        /// </summary>
        [NotMapped]
        public string DisplayName { get; set; }
    }
}
