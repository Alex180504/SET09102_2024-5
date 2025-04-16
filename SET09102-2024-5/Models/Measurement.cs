// Models/Measurement.cs (updated)
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
        public int QuantityId { get; set; }

        [ForeignKey("QuantityId")]
        public Measurand Measurand { get; set; }

        public ICollection<IncidentMeasurement> IncidentMeasurements { get; set; }
    }
}
