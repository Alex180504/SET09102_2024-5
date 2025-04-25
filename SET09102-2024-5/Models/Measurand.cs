using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SET09102_2024_5.Models
{
    public class Measurand
    {
        [Key]

        public int MeasurandId { get; set; }


        [StringLength(100)]
        public string QuantityType { get; set; }

        [StringLength(100)]
        public string QuantityName { get; set; }

        [StringLength(20)]
        public string Symbol { get; set; }

        [StringLength(50)]
        public string Unit { get; set; }

        public ICollection<Sensor> Sensors { get; set; }
        public ICollection<Measurement> Measurements { get; set; }
    }
}
