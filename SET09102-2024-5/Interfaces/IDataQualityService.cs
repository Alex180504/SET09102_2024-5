using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Interfaces
{
    public interface IDataQualityService
    {
        Task<QualityReport> RunChecksAsync(
            string category,
            string site,
            DateTime from,
            DateTime to);
    }
}

