using System;
using System.Threading.Tasks;

namespace SET09102_2024_5.Interfaces
{
    public interface IDatabaseInitializationService
    {
        Task InitializeDatabaseAsync();
        Task<bool> TestConnectionAsync();
        string ConnectionString { get; }
    }
}