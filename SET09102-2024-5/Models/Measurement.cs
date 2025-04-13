using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SET09102_2024_5.Models
{
    public class Measurement
    {
        public int MeasurementId { get; set; }
        public DateTime? Timestamp { get; set; }
        public string UnitOfMeasurement { get; set; }
        public float? Value { get; set; }

        public int QuantityId { get; set; }
        public PhysicalQuantity PhysicalQuantity { get; set; }

        public ICollection<IncidentMeasurement> IncidentMeasurements { get; set; }
    }
}
