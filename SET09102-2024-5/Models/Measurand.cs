// Models/Measurand.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SET09102_2024_5.Models
{
    public class Measurand
    {
        [Key]
        public int QuantityId { get; set; }

        [Required]
        public int SensorId { get; set; }

        [StringLength(100)]
        public string QuantityType { get; set; }

        [StringLength(100)]
        public string QuantityName { get; set; }

        [ForeignKey("SensorId")]
        public Sensor Sensor { get; set; }

        public ICollection<Measurement> Measurements { get; set; }
    }
}
