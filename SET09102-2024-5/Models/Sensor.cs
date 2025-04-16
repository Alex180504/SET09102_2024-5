// Models/Sensor.cs (updated)
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SET09102_2024_5.Models
{
    public class Sensor
    {
        [Key]
        public int SensorId { get; set; }

        [StringLength(100)]
        public string SensorType { get; set; }

        [StringLength(50)]
        public string Status { get; set; }

        public DateTime? DeploymentDate { get; set; }

        // Updated relationships
        public Configuration Configuration { get; set; }
        public SensorFirmware Firmware { get; set; }
        public ICollection<Measurand> Measurands { get; set; }
        public ICollection<Maintenance> Maintenances { get; set; }
    }
}
