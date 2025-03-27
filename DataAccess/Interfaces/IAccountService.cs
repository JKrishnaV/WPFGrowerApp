using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IAccountService : IDatabaseService
    {
        Task<List<Account>> GetAllAccountsAsync();
        Task<Account> GetAccountByNumberAsync(decimal number);
        Task<bool> SaveAccountAsync(Account account);
        Task<List<Account>> GetAccountsByYearAsync(decimal year);
    }
} 