using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IGrowerService : IDatabaseService
    {
        Task<List<GrowerSearchResult>> SearchGrowersAsync(string searchTerm);
        Task<Grower> GetGrowerByNumberAsync(decimal growerNumber);
        Task<bool> SaveGrowerAsync(Grower grower);
        Task<List<GrowerSearchResult>> GetAllGrowersAsync();
    }
} 