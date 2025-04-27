using System.Threading.Tasks;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// Base service interface that defines common functionality for all services
    /// </summary>
    public interface IBaseService
    {
        /// <summary>
        /// Initializes the service
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// Releases resources and performs cleanup operations
        /// </summary>
        Task CleanupAsync();
    }
}