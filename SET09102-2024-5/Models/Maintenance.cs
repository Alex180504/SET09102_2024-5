using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SET09102_2024_5.Models
{
    public class Maintenance
    {
        public int MaintenanceId { get; set; }
        public DateTime? MaintenanceDate { get; set; }
        public string MaintainerComments { get; set; }

        public int SensorId { get; set; }
        public Sensor Sensor { get; set; }

        public int MaintainerId { get; set; }
        public User Maintainer { get; set; }
    }
}
