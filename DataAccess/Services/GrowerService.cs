using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using WPFGrowerApp.DataAccess.Repositories;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Services
{
    public class GrowerService
    {
        private readonly GrowerRepository _growerRepository;
        private readonly AccountRepository _accountRepository;
        private readonly ChequeRepository _chequeRepository;

        public GrowerService(GrowerRepository growerRepository, AccountRepository accountRepository, ChequeRepository chequeRepository)
        {
            _growerRepository = growerRepository ?? throw new ArgumentNullException(nameof(growerRepository));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _chequeRepository = chequeRepository ?? throw new ArgumentNullException(nameof(chequeRepository));
        }

        public async Task<List<GrowerSearchResult>> SearchGrowersAsync(string searchTerm)
        {
            try
            {
                return await _growerRepository.SearchGrowersAsync(searchTerm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching for growers: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<GrowerSearchResult>();
            }
        }

        public async Task<Grower> GetGrowerAsync(decimal growerNumber)
        {
            try
            {
                return await _growerRepository.GetGrowerAsync(growerNumber);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving grower: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new Grower();
            }
        }
        
        public async Task<List<Grower>> GetAllGrowersAsync()
        {
            try
            {
                return await _growerRepository.GetAllGrowersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving growers: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<Grower>();
            }
        }
        
        public async Task<decimal> GetNextGrowerNumberAsync()
        {
            try
            {
                return await _growerRepository.GetNextGrowerNumberAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting next grower number: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return 1000; // Default starting number if error occurs
            }
        }
        
        public async Task AddGrowerAsync(Grower grower)
        {
            try
            {
                await _growerRepository.AddGrowerAsync(grower);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding grower: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Re-throw to handle in the ViewModel
            }
        }
        
        public async Task UpdateGrowerAsync(Grower grower)
        {
            try
            {
                await _growerRepository.UpdateGrowerAsync(grower);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating grower: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Re-throw to handle in the ViewModel
            }
        }
    }
}
