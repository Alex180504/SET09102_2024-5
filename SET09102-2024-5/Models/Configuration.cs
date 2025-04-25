using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SET09102_2024_5.Models
{
    public class Configuration
    {
        [Key]
        [ForeignKey("Sensor")]
        public int SensorId { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public float? Altitude { get; set; }

        public int? Orientation { get; set; } 

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

        public int? MeasurementFrequency { get; set; }
        public float? MinThreshold { get; set; }
        public float? MaxThreshold { get; set; }

        public Sensor Sensor { get; set; }
    }
}
