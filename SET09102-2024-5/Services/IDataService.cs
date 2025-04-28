using System.Collections.Generic;
using System.Threading.Tasks;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;

namespace SET09102_2024_5.Services
{
	public interface IDataService
	{
		Task<List<EnvironmentalDataModel>> GetHistoricalData(string category, string site);
	}
}
