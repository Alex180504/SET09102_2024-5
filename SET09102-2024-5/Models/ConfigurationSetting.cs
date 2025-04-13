using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SET09102_2024_5.Models
{
    public class ConfigurationSetting
    {
        public int SettingId { get; set; }
        public string SettingName { get; set; }
        public float? MinimumValue { get; set; }
        public float? MaximumValue { get; set; }
        public float? CurrentValue { get; set; }

        public int SensorId { get; set; }
        public Sensor Sensor { get; set; }
    }
}
