using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.Models.Entities;

namespace WPFGrowerApp.DataAccess.Repositories
{
    public interface IGrowerRepository
    {
        Task<List<GrowerEntity>> GetAllAsync();
        Task<GrowerEntity> GetByNumberAsync(decimal growerNumber);
        Task<List<GrowerEntity>> SearchAsync(string searchTerm);
        Task<bool> SaveAsync(GrowerEntity grower);
        Task<bool> DeleteAsync(decimal growerNumber);
    }

    public class GrowerRepository : IGrowerRepository
    {
        private readonly ApplicationDbContext _context;

        public GrowerRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<GrowerEntity>> GetAllAsync()
        {
            return await _context.Growers
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<GrowerEntity> GetByNumberAsync(decimal growerNumber)
        {
            return await _context.Growers
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.GrowerNumber == growerNumber);
        }

        public async Task<List<GrowerEntity>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            searchTerm = searchTerm.ToLower();

            return await _context.Growers
                .AsNoTracking()
                .Where(g => 
                    (g.GrowerName != null && g.GrowerName.ToLower().Contains(searchTerm)) ||
                    (g.ChequeName != null && g.ChequeName.ToLower().Contains(searchTerm)) ||
                    g.GrowerNumber.ToString().Contains(searchTerm))
                .OrderBy(g => g.GrowerName)
                .ToListAsync();
        }

        public async Task<bool> SaveAsync(GrowerEntity grower)
        {
            try
            {
                var existingGrower = await _context.Growers
                    .FirstOrDefaultAsync(g => g.GrowerNumber == grower.GrowerNumber);

                if (existingGrower == null)
                {
                    // New grower
                    grower.AddDate = DateTime.Now;
                    grower.AddTime = DateTime.Now.ToString("HHmmss");
                    grower.AddOperator = Environment.UserName;
                    
                    await _context.Growers.AddAsync(grower);
                }
                else
                {
                    // Update existing grower
                    _context.Entry(existingGrower).State = EntityState.Detached;
                    
                    grower.EditDate = DateTime.Now;
                    grower.EditTime = DateTime.Now.ToString("HHmmss");
                    grower.EditOperator = Environment.UserName;
                    
                    _context.Growers.Update(grower);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(decimal growerNumber)
        {
            try
            {
                var grower = await _context.Growers
                    .FirstOrDefaultAsync(g => g.GrowerNumber == growerNumber);

                if (grower == null)
                    return false;

                grower.DeleteDate = DateTime.Now;
                grower.DeleteTime = DateTime.Now.ToString("HHmmss");
                grower.DeleteOperator = Environment.UserName;

                _context.Growers.Remove(grower);
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
