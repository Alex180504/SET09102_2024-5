using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SET09102_2024_5.Models
{
    public class QualityReport
    {
        public int TotalRecords { get; set; }
        public List<MissingRecord> Missing { get; set; } = new();
        public List<OutOfRangeRecord> OutOfRange { get; set; } = new();
        public List<DuplicateRecord> Duplicates { get; set; } = new();
    }
    public record MissingRecord(DateTime Timestamp, string Parameter);
    public record OutOfRangeRecord(DateTime Timestamp, string Parameter, double Value, double Min, double Max);
    public record DuplicateRecord(DateTime Timestamp, string Parameter, int Count);
}

