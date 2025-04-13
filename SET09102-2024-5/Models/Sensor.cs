using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SET09102_2024_5.Models
{
    public class Sensor
    {
        public int SensorId { get; set; }
        public string SensorType { get; set; }
        public string Status { get; set; }
        public DateTime? DeploymentDate { get; set; }

        public ICollection<ConfigurationSetting> ConfigurationSettings { get; set; }
        public ICollection<Maintenance> Maintenances { get; set; }
        public ICollection<PhysicalQuantity> PhysicalQuantities { get; set; }
    }
}
