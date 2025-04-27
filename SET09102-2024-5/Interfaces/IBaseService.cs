using System;
using System.Threading.Tasks;

namespace SET09102_2024_5.Interfaces
{
    public interface IBaseService
    {
        // Common service functionality that can be used across different services
        Task<bool> InitializeAsync();
        Task<bool> IsReadyAsync();
        string GetServiceStatus();
        string GetServiceName();
    }
}