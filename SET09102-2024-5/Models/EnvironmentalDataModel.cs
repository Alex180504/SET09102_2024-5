namespace SET09102_2024_5.Models
{
    public class EnvironmentalDataModel
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string ParameterType { get; set; }
        public string DataCategory { get; set; }
        public string SensorSite { get; set; }
    }
}