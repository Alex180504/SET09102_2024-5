using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SET09102_2024_5.Models
{
    public class IncidentMeasurement
    {
        public int Id { get; set; }

        public int MeasurementId { get; set; }
        public Measurement Measurement { get; set; }

        public int IncidentId { get; set; }
        public Incident Incident { get; set; }
    }
}
