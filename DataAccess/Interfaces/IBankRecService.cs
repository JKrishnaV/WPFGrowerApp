using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IBankRecService : IDatabaseService
    {
        Task<List<BankRec>> GetAllBankRecsAsync();
        Task<BankRec> GetBankRecByDateAsync(DateTime acctDate);
        Task<List<BankRec>> GetBankRecsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> SaveBankRecAsync(BankRec bankRec);
    }
} 