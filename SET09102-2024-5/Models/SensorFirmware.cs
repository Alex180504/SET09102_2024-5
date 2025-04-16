// Models/SensorFirmware.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SET09102_2024_5.Models
{
    public class SensorFirmware
    {
        [Key]
        public int FirmwareId { get; set; }

        [Required]
        public int SensorId { get; set; }

        [StringLength(50)]
        public string FirmwareVersion { get; set; }
        public DateTime? LastUpdateDate { get; set; }

        [ForeignKey("SensorId")]
        public Sensor Sensor { get; set; }
    }
}
