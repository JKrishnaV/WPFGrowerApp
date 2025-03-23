using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using WPFGrowerApp.DataAccess.Repositories;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Repositories
{
    public class GrowerRepository
    {
        private readonly DapperConnectionManager _connectionManager;

        public GrowerRepository(DapperConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        public async Task<List<GrowerSearchResult>> SearchGrowersAsync(string searchTerm)
        {
            try
            {
                using (var connection = _connectionManager.CreateConnection())
                {
                    // Simulate database query with sample data for testing
                    await Task.Delay(500); // Simulate network delay
                    
                    var results = new List<GrowerSearchResult>
                    {
                        new GrowerSearchResult { GrowerNumber = 410, GrowerName = "1030511 B.C.LTD.", ChequeName = "1030511 B.C.LTD.", City = "SURREY", Phone = "604-767-2835" },
                        new GrowerSearchResult { GrowerNumber = 411, GrowerName = "SMITH FARMS", ChequeName = "SMITH FARMS", City = "ABBOTSFORD", Phone = "604-123-4567" },
                        new GrowerSearchResult { GrowerNumber = 412, GrowerName = "JOHNSON BERRIES", ChequeName = "JOHNSON BERRIES", City = "LANGLEY", Phone = "604-987-6543" },
                        new GrowerSearchResult { GrowerNumber = 413, GrowerName = "GREEN ACRES", ChequeName = "GREEN ACRES", City = "CHILLIWACK", Phone = "604-555-1212" },
                        new GrowerSearchResult { GrowerNumber = 414, GrowerName = "VALLEY ORGANICS", ChequeName = "VALLEY ORGANICS", City = "MISSION", Phone = "604-222-3333" }
                    };
                    
                    if (string.IsNullOrWhiteSpace(searchTerm))
                    {
                        return results;
                    }
                    
                    searchTerm = searchTerm.ToLower();
                    return results.FindAll(g => 
                        g.GrowerName.ToLower().Contains(searchTerm) || 
                        g.ChequeName.ToLower().Contains(searchTerm) || 
                        g.City.ToLower().Contains(searchTerm) || 
                        g.GrowerNumber.ToString().Contains(searchTerm));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching for growers: {ex.Message}", ex);
            }
        }

        public async Task<Grower> GetGrowerAsync(decimal growerNumber)
        {
            try
            {
                using (var connection = _connectionManager.CreateConnection())
                {
                    // Simulate database query with sample data for testing
                    await Task.Delay(500); // Simulate network delay
                    
                    // Return sample data for grower #410
                    if (growerNumber == 410)
                    {
                        return new Grower
                        {
                            GrowerNumber = 410,
                            GrowerName = "1030511 B.C.LTD.",
                            ChequeName = "1030511 B.C.LTD.",
                            Address = "16975 40TH AVENUE",
                            City = "SURREY",
                            Postal = "V3S 0L5",
                            Phone = "604-767-2835",
                            Acres = 0,
                            Notes = "This is a sample note for the grower.",
                            Contract = "",
                            Currency = 'C',
                            ContractLimit = 0,
                            PayGroup = 1,
                            OnHold = false,
                            PhoneAdditional1 = "",
                            OtherNames = "",
                            PhoneAdditional2 = "",
                            LYFresh = 0,
                            LYOther = 0,
                            Certified = "",
                            ChargeGST = false
                        };
                    }
                    
                    // Return sample data for other grower numbers
                    if (growerNumber == 411)
                    {
                        return new Grower
                        {
                            GrowerNumber = 411,
                            GrowerName = "SMITH FARMS",
                            ChequeName = "SMITH FARMS",
                            Address = "1234 FARM ROAD",
                            City = "ABBOTSFORD",
                            Postal = "V2T 1A1",
                            Phone = "604-123-4567",
                            Acres = 25,
                            Notes = "Organic certified since 2018.",
                            Contract = "ANNUAL",
                            Currency = 'C',
                            ContractLimit = 50000,
                            PayGroup = 1,
                            OnHold = false,
                            PhoneAdditional1 = "604-123-7890",
                            OtherNames = "SMITH ORGANIC FARMS",
                            PhoneAdditional2 = "",
                            LYFresh = 15000,
                            LYOther = 5000,
                            Certified = "YES",
                            ChargeGST = true
                        };
                    }
                    
                    // Return empty grower for other numbers
                    return new Grower { GrowerNumber = growerNumber };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving grower: {ex.Message}", ex);
            }
        }
        
        public async Task<List<Grower>> GetAllGrowersAsync()
        {
            try
            {
                using (var connection = _connectionManager.CreateConnection())
                {
                    // Simulate database query with sample data for testing
                    await Task.Delay(500); // Simulate network delay
                    
                    return new List<Grower>
                    {
                        new Grower { GrowerNumber = 410, GrowerName = "1030511 B.C.LTD.", ChequeName = "1030511 B.C.LTD." },
                        new Grower { GrowerNumber = 411, GrowerName = "SMITH FARMS", ChequeName = "SMITH FARMS" },
                        new Grower { GrowerNumber = 412, GrowerName = "JOHNSON BERRIES", ChequeName = "JOHNSON BERRIES" },
                        new Grower { GrowerNumber = 413, GrowerName = "GREEN ACRES", ChequeName = "GREEN ACRES" },
                        new Grower { GrowerNumber = 414, GrowerName = "VALLEY ORGANICS", ChequeName = "VALLEY ORGANICS" }
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all growers: {ex.Message}", ex);
            }
        }
        
        public async Task<decimal> GetNextGrowerNumberAsync()
        {
            try
            {
                using (var connection = _connectionManager.CreateConnection())
                {
                    // Simulate database query for next available grower number
                    await Task.Delay(200); // Simulate network delay
                    
                    // Return 415 as the next available number for testing
                    return 415;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting next grower number: {ex.Message}", ex);
            }
        }
        
        public async Task AddGrowerAsync(Grower grower)
        {
            try
            {
                using (var connection = _connectionManager.CreateConnection())
                {
                    // Simulate database insert
                    await Task.Delay(500); // Simulate network delay
                    
                    // In a real implementation, this would insert the grower into the database
                    Console.WriteLine($"Added grower: {grower.GrowerNumber} - {grower.GrowerName}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding grower: {ex.Message}", ex);
            }
        }
        
        public async Task UpdateGrowerAsync(Grower grower)
        {
            try
            {
                using (var connection = _connectionManager.CreateConnection())
                {
                    // Simulate database update
                    await Task.Delay(500); // Simulate network delay
                    
                    // In a real implementation, this would update the grower in the database
                    Console.WriteLine($"Updated grower: {grower.GrowerNumber} - {grower.GrowerName}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating grower: {ex.Message}", ex);
            }
        }
    }
}
