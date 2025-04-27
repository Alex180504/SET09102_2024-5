namespace SET09102_2024_5.Models
{
    public class EnvironmentalDataModel
    {
        public DateTime Timestamp { get; set; }
        public string SensorSite { get; set; }
        public string DataCategory { get; set; }    // “Air”/“Water”/“Weather”
        public string ParameterType { get; set; }    // e.g. “Nitrogen dioxide”
        public Dictionary<string, double> Values { get; set; } = new();

        public EnvironmentalDataModel()
        {
            Values = new Dictionary<string, double>();
        }
    }
}