using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;

namespace SET09102_2024_5.Services
{
    public class MockDataService : IDataService
    {
        public async Task<List<EnvironmentalDataModel>> GetHistoricalData(string category, string site)
        {
            // Simulate delay
            await Task.Delay(500);

            var random = new Random();
            var list = new List<EnvironmentalDataModel>();

            for (int i = 0; i < 10; i++)
            {
                list.Add(new EnvironmentalDataModel
                {
                    Timestamp = DateTime.Now.AddDays(-i),
                    Value = Math.Round(random.NextDouble() * 100, 2),
                    ParameterType = "Temperature",
                    DataCategory = category,
                    SensorSite = site
                });
            }

            list.Reverse(); // so it goes oldest ➝ newest
            return list;
        }
    }
}
