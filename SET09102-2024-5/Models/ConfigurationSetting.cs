using System.ComponentModel.DataAnnotations;

namespace SET09102_2024_5.Models
{
    public class ConfigurationSetting
    {
        [Key]
        public int ConfigurationSettingId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }
}