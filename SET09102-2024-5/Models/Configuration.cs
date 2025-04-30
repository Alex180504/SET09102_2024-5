using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SET09102_2024_5.Models
{
    /// <summary>
    /// Represents the configuration settings for a sensor in the monitoring system.
    /// Contains location, orientation, and operational parameters.
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Primary key and foreign key to the associated Sensor.
        /// </summary>
        [Key]
        [ForeignKey("Sensor")]
        public int SensorId { get; set; }

        /// <summary>
        /// The latitude coordinate of the sensor's physical location.
        /// </summary>
        public float? Latitude { get; set; }

        /// <summary>
        /// The longitude coordinate of the sensor's physical location.
        /// </summary>
        public float? Longitude { get; set; }

        /// <summary>
        /// The altitude of the sensor in meters above sea level.
        /// </summary>
        public float? Altitude { get; set; }

        /// <summary>
        /// The orientation angle of the sensor in degrees.
        /// </summary>
        public int? Orientation { get; set; }

        /// <summary>
        /// Provides a formatted display value for the sensor's orientation with degree symbol.
        /// Not stored in the database.
        /// </summary>
        [NotMapped]
        public string OrientationDisplay
        {
            get => Orientation.HasValue ? $"{Orientation}°" : null;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Orientation = null;
                    return;
                }

                string orientationValue = value.TrimEnd('°');
                if (int.TryParse(orientationValue, out int degrees))
                {
                    Orientation = degrees;
                }
            }
        }

        /// <summary>
        /// The frequency at which the sensor takes measurements, in seconds.
        /// </summary>
        public int? MeasurementFrequency { get; set; }

        /// <summary>
        /// The minimum threshold value for triggering alerts or actions.
        /// </summary>
        public float? MinThreshold { get; set; }

        /// <summary>
        /// The maximum threshold value for triggering alerts or actions.
        /// </summary>
        public float? MaxThreshold { get; set; }

        /// <summary>
        /// Navigation property to the associated Sensor entity.
        /// </summary>
        public Sensor Sensor { get; set; }
    }
}
