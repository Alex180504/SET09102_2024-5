using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Required]
        public int MeasurandId { get; set; }

        [ForeignKey("MeasurandId")]
        public Measurand Measurand { get; set; }

        public Configuration Configuration { get; set; }
        public SensorFirmware Firmware { get; set; }
        public ICollection<Measurement> Measurements { get; set; }
        public ICollection<Maintenance> Maintenances { get; set; }

        [NotMapped]
        public string DisplayName { get; set; }
    }
}