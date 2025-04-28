using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SET09102_2024_5.Models
{
    public class Measurement
    {
        [Key]
        public int MeasurementId { get; set; }

        public DateTime? Timestamp { get; set; }
        public float? Value { get; set; }

        [Required]
        public int SensorId { get; set; }

        [ForeignKey("SensorId")]
        public Sensor Sensor { get; set; }
        
        [Required]
        public int PhysicalQuantityId { get; set; }
        
        [ForeignKey("PhysicalQuantityId")]
        public PhysicalQuantity PhysicalQuantity { get; set; }

        public ICollection<IncidentMeasurement> IncidentMeasurements { get; set; } = new List<IncidentMeasurement>();
    }
}
