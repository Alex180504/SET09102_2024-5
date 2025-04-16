using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SET09102_2024_5.Models
{
    public class Configuration
    {
        [Key]
        public int ConfigId { get; set; }
        
        [Required]
        public int SensorId { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public float? Altitude { get; set; }
        
        [StringLength(50)]
        public string Orientation { get; set; }
        public int? MeasurementFrequency { get; set; }
        public float? MinThreshold { get; set; }
        public float? MaxThreshold { get; set; }
        
        [StringLength(100)]
        public string ReadingFormat { get; set; }
        
        [ForeignKey("SensorId")]
        public Sensor Sensor { get; set; }
    }
}