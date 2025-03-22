using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.Models.Entities;

namespace WPFGrowerApp.DataAccess.Repositories
{
    public interface IAccountRepository
    {
        Task<List<AccountEntity>> GetAllAsync();
        Task<AccountEntity> GetByNumberAsync(decimal accountNumber);
        Task<List<AccountEntity>> GetByGrowerNumberAsync(decimal growerNumber);
        Task<bool> SaveAsync(AccountEntity account);
        Task<bool> DeleteAsync(decimal accountNumber);
    }

    public class AccountRepository : IAccountRepository
    {
        private readonly ApplicationDbContext _context;

        public AccountRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<AccountEntity>> GetAllAsync()
        {
            return await _context.Accounts
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<AccountEntity> GetByNumberAsync(decimal accountNumber)
        {
            return await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Number == accountNumber);
        }

        public async Task<List<AccountEntity>> GetByGrowerNumberAsync(decimal growerNumber)
        {
            return await _context.Accounts
                .AsNoTracking()
                .Where(a => a.Number == growerNumber)
                .ToListAsync();
        }

        public async Task<bool> SaveAsync(AccountEntity account)
        {
            try
            {
                var existingAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Number == account.Number);

                if (existingAccount == null)
                {
                    // New account
                    account.AddDate = DateTime.Now;
                    account.AddTime = DateTime.Now.ToString("HHmmss");
                    account.AddOperator = Environment.UserName;
                    
                    await _context.Accounts.AddAsync(account);
                }
                else
                {
                    // Update existing account
                    _context.Entry(existingAccount).State = EntityState.Detached;
                    
                    account.EditDate = DateTime.Now;
                    account.EditTime = DateTime.Now.ToString("HHmmss");
                    account.EditOperator = Environment.UserName;
                    
                    _context.Accounts.Update(account);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(decimal accountNumber)
        {
            try
            {
                var account = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Number == accountNumber);

                if (account == null)
                    return false;

                account.DeleteDate = DateTime.Now;
                account.DeleteTime = DateTime.Now.ToString("HHmmss");
                account.DeleteOperator = Environment.UserName;

                _context.Accounts.Remove(account);
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
