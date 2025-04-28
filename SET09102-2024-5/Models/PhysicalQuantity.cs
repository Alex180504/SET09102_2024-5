using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SET09102_2024_5.Models
{
    public class PhysicalQuantity
    {
        [Key]
        public int PhysicalQuantityId { get; set; }
        
        [StringLength(100)]
        public string QuantityType { get; set; }
        
        [StringLength(100)]
        public string QuantityName { get; set; }
        
        [StringLength(20)]
        public string Symbol { get; set; }
        
        [StringLength(50)]
        public string Unit { get; set; }
        
        public int SensorId { get; set; }
        
        [ForeignKey("SensorId")]
        public Sensor Sensor { get; set; }
    }
}