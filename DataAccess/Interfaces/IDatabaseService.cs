using System.Threading.Tasks;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IDatabaseService
    {
        Task<bool> TestConnectionAsync();
    }
} 