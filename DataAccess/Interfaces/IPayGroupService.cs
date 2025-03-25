using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IPayGroupService : IDatabaseService
    {
        Task<List<PayGroup>> GetPayGroupsAsync();
    }
} 