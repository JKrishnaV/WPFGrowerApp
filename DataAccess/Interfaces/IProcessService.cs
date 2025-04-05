using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Defines operations for retrieving Process information.
    /// </summary>
    public interface IProcessService
    {
        /// <summary>
        /// Gets a list of all available processes.
        /// </summary>
        /// <returns>A list of Process objects.</returns>
        Task<List<Process>> GetAllProcessesAsync();
    }
}
