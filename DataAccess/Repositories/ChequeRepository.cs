using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Repositories
{
    public class ChequeRepository
    {
        private readonly DapperConnectionManager _connectionManager;

        public ChequeRepository(DapperConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        public async Task<List<Cheque>> GetChequesForGrowerAsync(decimal growerNumber)
        {
            try
            {
                using (IDbConnection connection = _connectionManager.CreateConnection())
                {
                    string query = @"
                        SELECT ChequeID, GrowerNumber, ChequeNumber, ChequeDate, Amount, Status, Notes
                        FROM Cheques
                        WHERE GrowerNumber = @GrowerNumber
                        ORDER BY ChequeDate DESC";

                    var cheques = await connection.QueryAsync<Cheque>(query, new { GrowerNumber = growerNumber });
                    return cheques.AsList();
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error retrieving cheques: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving cheques: {ex.Message}", ex);
            }
        }

        public async Task<Cheque> GetChequeByIdAsync(int chequeId)
        {
            try
            {
                using (IDbConnection connection = _connectionManager.CreateConnection())
                {
                    string query = @"
                        SELECT ChequeID, GrowerNumber, ChequeNumber, ChequeDate, Amount, Status, Notes
                        FROM Cheques
                        WHERE ChequeID = @ChequeID";

                    return await connection.QueryFirstOrDefaultAsync<Cheque>(query, new { ChequeID = chequeId });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error retrieving cheque: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving cheque: {ex.Message}", ex);
            }
        }

        public async Task AddChequeAsync(Cheque cheque)
        {
            try
            {
                using (IDbConnection connection = _connectionManager.CreateConnection())
                {
                    string query = @"
                        INSERT INTO Cheques (GrowerNumber, ChequeNumber, ChequeDate, Amount, Status, Notes)
                        VALUES (@GrowerNumber, @ChequeNumber, @ChequeDate, @Amount, @Status, @Notes);
                        SELECT CAST(SCOPE_IDENTITY() as int)";

                    int chequeId = await connection.QuerySingleAsync<int>(query, cheque);
                    cheque.ChequeID = chequeId;
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error adding cheque: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding cheque: {ex.Message}", ex);
            }
        }

        public async Task UpdateChequeAsync(Cheque cheque)
        {
            try
            {
                using (IDbConnection connection = _connectionManager.CreateConnection())
                {
                    string query = @"
                        UPDATE Cheques
                        SET GrowerNumber = @GrowerNumber,
                            ChequeNumber = @ChequeNumber,
                            ChequeDate = @ChequeDate,
                            Amount = @Amount,
                            Status = @Status,
                            Notes = @Notes
                        WHERE ChequeID = @ChequeID";

                    await connection.ExecuteAsync(query, cheque);
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error updating cheque: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating cheque: {ex.Message}", ex);
            }
        }

        public async Task DeleteChequeAsync(int chequeId)
        {
            try
            {
                using (IDbConnection connection = _connectionManager.CreateConnection())
                {
                    string query = "DELETE FROM Cheques WHERE ChequeID = @ChequeID";
                    await connection.ExecuteAsync(query, new { ChequeID = chequeId });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error deleting cheque: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting cheque: {ex.Message}", ex);
            }
        }
    }
}
