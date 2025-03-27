using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IAuditService : IDatabaseService
    {
        Task<List<Audit>> GetAllAuditsAsync();
        Task<Audit> GetAuditByDayUniqAsync(decimal dayUniq);
        Task<List<Audit>> GetAuditsByAccountUniqAsync(decimal acctUniq);
        Task<bool> SaveAuditAsync(Audit audit);
    }
} 