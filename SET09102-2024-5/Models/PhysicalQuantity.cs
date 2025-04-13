using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SET09102_2024_5.Models
{
    public class PhysicalQuantity
    {
        public int QuantityId { get; set; }
        public string QuantityName { get; set; }
        public float? LowerWarningThreshold { get; set; }
        public float? UpperWarningThreshold { get; set; }
        public float? LowerEmergencyThreshold { get; set; }
        public float? UpperEmergencyThreshold { get; set; }

        public int SensorId { get; set; }
        public Sensor Sensor { get; set; }

        public ICollection<Measurement> Measurements { get; set; }
    }
}
