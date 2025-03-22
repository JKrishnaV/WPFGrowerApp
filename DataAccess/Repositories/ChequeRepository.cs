using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.Models.Entities;

namespace WPFGrowerApp.DataAccess.Repositories
{
    public interface IChequeRepository
    {
        Task<List<ChequeEntity>> GetAllAsync();
        Task<ChequeEntity> GetByIdAsync(string series, decimal chequeNumber);
        Task<List<ChequeEntity>> GetByGrowerNumberAsync(decimal growerNumber);
        Task<bool> SaveAsync(ChequeEntity cheque);
        Task<bool> DeleteAsync(string series, decimal chequeNumber);
    }

    public class ChequeRepository : IChequeRepository
    {
        private readonly ApplicationDbContext _context;

        public ChequeRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<ChequeEntity>> GetAllAsync()
        {
            return await _context.Cheques
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ChequeEntity> GetByIdAsync(string series, decimal chequeNumber)
        {
            return await _context.Cheques
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Series == series && c.Cheque == chequeNumber);
        }

        public async Task<List<ChequeEntity>> GetByGrowerNumberAsync(decimal growerNumber)
        {
            return await _context.Cheques
                .AsNoTracking()
                .Where(c => c.Number == growerNumber)
                .ToListAsync();
        }

        public async Task<bool> SaveAsync(ChequeEntity cheque)
        {
            try
            {
                var existingCheque = await _context.Cheques
                    .FirstOrDefaultAsync(c => c.Series == cheque.Series && c.Cheque == cheque.Cheque);

                if (existingCheque == null)
                {
                    // New cheque
                    cheque.AddDate = DateTime.Now;
                    cheque.AddTime = DateTime.Now.ToString("HHmmss");
                    cheque.AddOperator = Environment.UserName;
                    
                    await _context.Cheques.AddAsync(cheque);
                }
                else
                {
                    // Update existing cheque
                    _context.Entry(existingCheque).State = EntityState.Detached;
                    
                    cheque.EditDate = DateTime.Now;
                    cheque.EditTime = DateTime.Now.ToString("HHmmss");
                    cheque.EditOperator = Environment.UserName;
                    
                    _context.Cheques.Update(cheque);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string series, decimal chequeNumber)
        {
            try
            {
                var cheque = await _context.Cheques
                    .FirstOrDefaultAsync(c => c.Series == series && c.Cheque == chequeNumber);

                if (cheque == null)
                    return false;

                cheque.DeleteDate = DateTime.Now;
                cheque.DeleteTime = DateTime.Now.ToString("HHmmss");
                cheque.DeleteOperator = Environment.UserName;

                _context.Cheques.Remove(cheque);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
