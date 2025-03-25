using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using WPFGrowerApp.DataAccess;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.Reports
{
    public class ReportDataManager
    {
        private readonly DatabaseService _databaseService;

        public ReportDataManager(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        public async Task<DataTable> GetGrowerSummaryDataAsync()
        {
            var growers = await _databaseService.GetGrowersAsync();
            var dataTable = new DataTable();

            // Define columns
            dataTable.Columns.Add("GrowerNumber", typeof(decimal));
            dataTable.Columns.Add("GrowerName", typeof(string));
            dataTable.Columns.Add("Address", typeof(string));
            dataTable.Columns.Add("City", typeof(string));
            dataTable.Columns.Add("Prov", typeof(string));
            dataTable.Columns.Add("Pcode", typeof(string));
            dataTable.Columns.Add("Phone", typeof(string));
            dataTable.Columns.Add("Currency", typeof(string));
            dataTable.Columns.Add("PayGroup", typeof(string));

            // Populate data
            foreach (var grower in growers)
            {
                var currency = grower.Currency == 'U' ? "USD" : "CAD";
                dataTable.Rows.Add(
                    grower.GrowerNumber,
                    grower.GrowerName,
                    grower.Address,
                    grower.City,
                    grower.Prov,
                    grower.Postal,
                    grower.Phone,
                    currency,
                    grower.PayGroup
                );
            }

            return dataTable;
        }

        public async Task<DataTable> GetGrowerDetailsDataAsync(string growerId)
        {
            var grower = await _databaseService.GetGrowerByIdAsync(growerId);
            if (grower == null)
                throw new ArgumentException($"Grower not found: {growerId}");

            var dataTable = new DataTable();

            // Define columns for detailed view
            dataTable.Columns.Add("GrowerNumber", typeof(decimal));
            dataTable.Columns.Add("GrowerName", typeof(string));
            dataTable.Columns.Add("ChequeName", typeof(string));
            dataTable.Columns.Add("Address", typeof(string));
            dataTable.Columns.Add("City", typeof(string));
            dataTable.Columns.Add("Prov", typeof(string));
            dataTable.Columns.Add("Pcode", typeof(string));
            dataTable.Columns.Add("Phone", typeof(string));
            dataTable.Columns.Add("PhoneAdditional1", typeof(string));
            dataTable.Columns.Add("PhoneAdditional2", typeof(string));
            dataTable.Columns.Add("Currency", typeof(string));
            dataTable.Columns.Add("PayGroup", typeof(string));
            dataTable.Columns.Add("PriceLevel", typeof(int));
            dataTable.Columns.Add("Notes", typeof(string));
            dataTable.Columns.Add("OnHold", typeof(bool));

            // Add the single grower row
            var currency = grower.Currency == 'U' ? "USD" : "CAD";
            dataTable.Rows.Add(
                grower.GrowerNumber,
                grower.GrowerName,
                grower.ChequeName,
                grower.Address,
                grower.City,
                grower.Prov,
                grower.Postal,
                grower.Phone,
                grower.PhoneAdditional1,
                grower.PhoneAdditional2,
                currency,
                grower.PayGroup,
                grower.PriceLevel,
                grower.Notes,
                grower.OnHold
            );

            return dataTable;
        }

        public async Task<DataTable> GetFinancialSummaryDataAsync()
        {
            var growers = await _databaseService.GetGrowersAsync();
            var payGroups = await _databaseService.GetPayGroupsAsync();
            
            var dataTable = new DataTable();

            // Define columns for financial summary
            dataTable.Columns.Add("PayGroup", typeof(string));
            dataTable.Columns.Add("Description", typeof(string));
            dataTable.Columns.Add("DefaultPriceLevel", typeof(string));
            dataTable.Columns.Add("GrowerCount", typeof(int));
            dataTable.Columns.Add("USDCount", typeof(int));
            dataTable.Columns.Add("CADCount", typeof(int));

            // Create a dictionary to store pay group statistics
            var stats = new Dictionary<string, (string Description, string DefaultPriceLevel, int Total, int USD, int CAD)>();

            // Initialize stats from pay groups
            foreach (var pg in payGroups)
            {
                stats[pg.PayGroupId] = (pg.Description, pg.DefaultPriceLevel, 0, 0, 0);
            }

            // Calculate statistics
            foreach (var grower in growers)
            {
                if (stats.ContainsKey(grower.PayGroup))
                {
                    var current = stats[grower.PayGroup];
                    var newTotal = current.Total + 1;
                    var newUSD = current.USD + (grower.Currency == 'U' ? 1 : 0);
                    var newCAD = current.CAD + (grower.Currency == 'C' ? 1 : 0);
                    stats[grower.PayGroup] = (current.Description, current.DefaultPriceLevel, newTotal, newUSD, newCAD);
                }
            }

            // Populate data table
            foreach (var kvp in stats)
            {
                dataTable.Rows.Add(
                    kvp.Key,
                    kvp.Value.Description,
                    kvp.Value.DefaultPriceLevel,
                    kvp.Value.Total,
                    kvp.Value.USD,
                    kvp.Value.CAD
                );
            }

            return dataTable;
        }
    }
} 