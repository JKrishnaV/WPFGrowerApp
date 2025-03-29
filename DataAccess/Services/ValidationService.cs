using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using WPFGrowerApp.DataAccess.Exceptions;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Services
{
    public class ValidationService : BaseDatabaseService
    {
        private const decimal MIN_WEIGHT = 0.01m;
        private const decimal MAX_WEIGHT = 100000m;
        private const decimal MIN_PRICE = 0.01m;
        private const decimal MAX_PRICE = 1000m;

        public async Task ValidateReceiptAsync(Receipt receipt)
        {
            var errors = new List<string>();

            // Validate grower
            if (!await ValidateGrowerAsync(receipt.GrowerNumber))
            {
                errors.Add($"Invalid grower number: {receipt.GrowerNumber}");
            }

            // Validate product
            if (!await ValidateProductAsync(receipt.Product))
            {
                errors.Add($"Invalid product code: {receipt.Product}");
            }

            // Validate process
            if (!await ValidateProcessAsync(receipt.Process))
            {
                errors.Add($"Invalid process code: {receipt.Process}");
            }

            // Validate grade
            if (!await ValidateGradeAsync(receipt.Grade))
            {
                errors.Add($"Invalid grade code: {receipt.Grade}");
            }

            // Validate weights
            if (receipt.Gross < MIN_WEIGHT || receipt.Gross > MAX_WEIGHT)
            {
                errors.Add($"Gross weight {receipt.Gross} is outside valid range ({MIN_WEIGHT}-{MAX_WEIGHT})");
            }

            if (receipt.Tare < 0 || receipt.Tare > receipt.Gross)
            {
                errors.Add($"Invalid tare weight: {receipt.Tare}");
            }

            if (receipt.Net < MIN_WEIGHT || receipt.Net > MAX_WEIGHT)
            {
                errors.Add($"Net weight {receipt.Net} is outside valid range ({MIN_WEIGHT}-{MAX_WEIGHT})");
            }

            // Validate price
            if (receipt.ThePrice < MIN_PRICE || receipt.ThePrice > MAX_PRICE)
            {
                errors.Add($"Price {receipt.ThePrice} is outside valid range ({MIN_PRICE}-{MAX_PRICE})");
            }

            // Check for duplicate receipt number
            if (await IsDuplicateReceiptAsync(receipt.ReceiptNumber))
            {
                errors.Add($"Duplicate receipt number: {receipt.ReceiptNumber}");
            }

            if (errors.Count > 0)
            {
                throw new ImportValidationException("Receipt validation failed", errors);
            }
        }

        private async Task<bool> ValidateGrowerAsync(decimal growerNumber)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Grower WHERE NUMBER = @GrowerNumber",
                    new { GrowerNumber = growerNumber });
                return count > 0;
            }
        }

        private async Task<bool> ValidateProductAsync(string product)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Product WHERE PRODUCT = @Product",
                    new { Product = product });
                return count > 0;
            }
        }

        private async Task<bool> ValidateProcessAsync(string process)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Process WHERE PROCESS = @Process",
                    new { Process = process });
                return count > 0;
            }
        }

        private async Task<bool> ValidateGradeAsync(string grade)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Grade WHERE GRADE = @Grade",
                    new { Grade = grade });
                return count > 0;
            }
        }

        private async Task<bool> IsDuplicateReceiptAsync(decimal receiptNumber)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Daily WHERE RECPT = @ReceiptNumber",
                    new { ReceiptNumber = receiptNumber });
                return count > 0;
            }
        }

        public async Task ValidateImportBatchAsync(ImportBatch importBatch)
        {
            var errors = new List<string>();

            // Validate depot
            if (!await ValidateDepotAsync(importBatch.Depot))
            {
                errors.Add($"Invalid depot code: {importBatch.Depot}");
            }

            // Validate import file name
            if (string.IsNullOrWhiteSpace(importBatch.ImpFile))
            {
                errors.Add("Import file name is required");
            }

            // Check for duplicate import batch number
            if (await IsDuplicateImportBatchAsync(importBatch.ImpBatch))
            {
                errors.Add($"Duplicate import batch number: {importBatch.ImpBatch}");
            }

            if (errors.Count > 0)
            {
                throw new ImportValidationException("Import batch validation failed", errors);
            }
        }

        private async Task<bool> ValidateDepotAsync(string depot)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Depot WHERE DEPOT = @Depot",
                    new { Depot = depot });
                return count > 0;
            }
        }

        private async Task<bool> IsDuplicateImportBatchAsync(decimal impBatch)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var count = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM ImpBat WHERE IMP_BAT = @ImpBatch",
                    new { ImpBatch = impBatch });
                return count > 0;
            }
        }
    }
} 