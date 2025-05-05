using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SET09102_2024_5.Models
{
    /// <summary>
    /// Represents a physical quantity that can be measured by sensors in the monitoring system.
    /// Contains information about the quantity type, name, symbol, and unit of measurement.
    /// </summary>
    public class Measurand
    {
        /// <summary>
        /// The unique identifier for the measurand.
        /// </summary>
        [Key]
        public int MeasurandId { get; set; }

        /// <summary>
        /// The category or classification of the physical quantity being measured.
        /// For example: "Temperature", "Pressure", "Humidity", etc.
        /// Maximum length: 100 characters.
        /// </summary>
        [StringLength(100)]
        public string QuantityType { get; set; }

        /// <summary>
        /// The specific name of the physical quantity being measured.
        /// Provides a more detailed description than the QuantityType.
        /// Maximum length: 100 characters.
        /// </summary>
        [StringLength(100)]
        public string QuantityName { get; set; }

        /// <summary>
        /// The scientific or standard symbol used to represent this physical quantity.
        /// For example: "T" for temperature, "P" for pressure, etc.
        /// Maximum length: 20 characters.
        /// </summary>
        [StringLength(20)]
        public string Symbol { get; set; }

        /// <summary>
        /// The unit of measurement for this physical quantity.
        /// For example: "Celsius", "Pascals", "%RH", etc.
        /// Maximum length: 50 characters.
        /// </summary>
        [StringLength(50)]
        public string Unit { get; set; }

        /// <summary>
        /// Collection of sensors that measure this physical quantity.
        /// Represents a one-to-many relationship from Measurand to Sensor.
        /// </summary>
        public ICollection<Sensor> Sensors { get; set; }

        /// <summary>
        /// Collection of measurements recorded for this physical quantity.
        /// Represents a one-to-many relationship from Measurand to Measurement.
        /// </summary>
        public ICollection<Measurement> Measurements { get; set; }
    }
}
