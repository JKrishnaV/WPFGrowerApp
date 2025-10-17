using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for managing electronic payment operations.
    /// </summary>
    public class ElectronicPaymentService : BaseDatabaseService, IElectronicPaymentService
    {
        private readonly NachaFileGenerator _nachaFileGenerator;

        public ElectronicPaymentService()
        {
            _nachaFileGenerator = new NachaFileGenerator();
        }

        public async Task<byte[]> GenerateNachaFileAsync(List<int> electronicPaymentIds)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT ep.*, g.GrowerName, g.BankAccountNumber, g.RoutingNumber
                    FROM ElectronicPayments ep
                    INNER JOIN Growers g ON ep.GrowerId = g.GrowerId
                    WHERE ep.ElectronicPaymentId IN @PaymentIds
                    AND ep.Status = 'Generated'
                    ORDER BY ep.ElectronicPaymentId";

                var payments = await connection.QueryAsync<ElectronicPayment>(sql, new { PaymentIds = electronicPaymentIds });
                var paymentList = payments.ToList();

                if (!paymentList.Any())
                {
                    Logger.Warn("No electronic payments found for NACHA file generation");
                    return Array.Empty<byte>();
                }

                var nachaFile = await _nachaFileGenerator.GenerateNachaFileAsync(paymentList, "COMPANY001", "Berry Farms", "123456789", "987654321");
                Logger.Info($"Generated NACHA file for {paymentList.Count} electronic payments");
                return nachaFile;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating NACHA file: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<ElectronicPaymentFile> SaveElectronicPaymentFileAsync(ElectronicPaymentFile file)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO ElectronicPaymentFiles (
                        FileName, FileFormat, FileContent, TotalAmount, TotalPayments,
                        GeneratedDate, GeneratedBy, Status, Notes
                    )
                    VALUES (
                        @FileName, @FileFormat, @FileContent, @TotalAmount, @TotalPayments,
                        @GeneratedDate, @GeneratedBy, @Status, @Notes
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var fileId = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    FileName = file.FileName,
                    FileFormat = file.FileFormat,
                    FileContent = file.FileContent,
                    TotalAmount = file.TotalAmount,
                    TotalPayments = file.TotalPayments,
                    GeneratedDate = DateTime.Now,
                    GeneratedBy = App.CurrentUser?.Username ?? "SYSTEM",
                    Status = file.Status,
                    Notes = file.Notes
                });

                file.FileId = fileId;
                Logger.Info($"Saved electronic payment file: {file.FileName}, ID: {fileId}");
                return file;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error saving electronic payment file: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<ElectronicPayment>> GetPendingElectronicPaymentsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT ep.*, g.GrowerName
                    FROM ElectronicPayments ep
                    INNER JOIN Growers g ON ep.GrowerId = g.GrowerId
                    WHERE ep.Status = 'Generated'
                    ORDER BY ep.PaymentDate DESC";

                var payments = await connection.QueryAsync<ElectronicPayment>(sql);
                return payments.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving pending electronic payments: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> MarkPaymentsAsProcessedAsync(List<int> paymentIds, string processedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE ElectronicPayments 
                    SET Status = 'Processed', ProcessedAt = @ProcessedAt, ProcessedBy = @ProcessedBy
                    WHERE ElectronicPaymentId IN @PaymentIds";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    ProcessedAt = DateTime.Now,
                    ProcessedBy = processedBy,
                    PaymentIds = paymentIds
                });

                Logger.Info($"Marked {rowsAffected} electronic payments as processed");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking payments as processed: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<ElectronicPayment>> GetAllElectronicPaymentsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT ep.*, g.GrowerName
                    FROM ElectronicPayments ep
                    INNER JOIN Growers g ON ep.GrowerId = g.GrowerId
                    ORDER BY ep.PaymentDate DESC";

                var payments = await connection.QueryAsync<ElectronicPayment>(sql);
                return payments.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving all electronic payments: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<ElectronicPaymentFile>> GetElectronicPaymentFilesAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT *
                    FROM ElectronicPaymentFiles
                    ORDER BY GeneratedDate DESC";

                var files = await connection.QueryAsync<ElectronicPaymentFile>(sql);
                return files.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving electronic payment files: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<ElectronicPaymentFile> GetElectronicPaymentFileAsync(int fileId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT *
                    FROM ElectronicPaymentFiles
                    WHERE FileId = @FileId";

                var file = await connection.QueryFirstOrDefaultAsync<ElectronicPaymentFile>(sql, new { FileId = fileId });
                return file ?? throw new InvalidOperationException($"Electronic payment file {fileId} not found");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving electronic payment file {fileId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> UpdateFileStatusAsync(int fileId, string newStatus, string processedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE ElectronicPaymentFiles 
                    SET Status = @Status, ProcessedDate = @ProcessedDate, ProcessedBy = @ProcessedBy
                    WHERE FileId = @FileId";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    Status = newStatus,
                    ProcessedDate = DateTime.Now,
                    ProcessedBy = processedBy,
                    FileId = fileId
                });

                Logger.Info($"Updated file status for file {fileId} to {newStatus}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating file status: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<string>> ValidateElectronicPaymentsAsync(List<int> paymentIds)
        {
            try
            {
                var errors = new List<string>();
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT ep.*, g.GrowerName, g.BankAccountNumber, g.RoutingNumber
                    FROM ElectronicPayments ep
                    INNER JOIN Growers g ON ep.GrowerId = g.GrowerId
                    WHERE ep.ElectronicPaymentId IN @PaymentIds";

                var payments = await connection.QueryAsync(sql, new { PaymentIds = paymentIds });
                var paymentList = payments.ToList();

                foreach (var payment in paymentList)
                {
                    if (string.IsNullOrEmpty(payment.BankAccountNumber))
                        errors.Add($"Grower {payment.GrowerName} has no bank account number");
                    if (string.IsNullOrEmpty(payment.RoutingNumber))
                        errors.Add($"Grower {payment.GrowerName} has no routing number");
                    if (payment.Amount <= 0)
                        errors.Add($"Payment for {payment.GrowerName} has invalid amount: {payment.Amount}");
                }

                return errors;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating electronic payments: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetElectronicPaymentStatisticsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        Status,
                        COUNT(*) as Count,
                        SUM(Amount) as TotalAmount
                    FROM ElectronicPayments
                    GROUP BY Status";

                var results = await connection.QueryAsync(sql);
                var stats = new Dictionary<string, object>();
                
                foreach (var result in results)
                {
                    stats[$"{result.Status}_Count"] = result.Count;
                    stats[$"{result.Status}_Amount"] = result.TotalAmount;
                }

                return stats;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving electronic payment statistics: {ex.Message}", ex);
                throw;
            }
        }
    }
}
