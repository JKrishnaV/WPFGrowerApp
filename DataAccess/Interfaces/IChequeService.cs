using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IChequeService : IDatabaseService
    {
        Task<List<Cheque>> GetAllChequesAsync();
        Task<Cheque> GetChequeBySeriesAndNumberAsync(string series, decimal chequeNumber);
        Task<List<Cheque>> GetChequesByGrowerNumberAsync(decimal growerNumber);
        Task<List<Cheque>> GetChequesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> SaveChequeAsync(Cheque cheque);
        Task<bool> VoidChequeAsync(string series, decimal chequeNumber);
    }
} 