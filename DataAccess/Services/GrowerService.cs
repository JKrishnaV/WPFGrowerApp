using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for grower data operations with complete CRUD functionality.
    /// Implements all methods from IGrowerService interface.
    /// </summary>
    public class GrowerService : BaseDatabaseService, IGrowerService
    {
        public GrowerService() : base() { }

        #region Core CRUD Operations

        public async Task<Grower> GetGrowerByIdAsync(int growerId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            g.GrowerId,
                            g.GrowerNumber,
                            g.FullName,
                            g.CheckPayeeName,
                            g.Address,
                            g.City,
                            g.Province,
                            g.PostalCode,
                            g.PhoneNumber,
                            g.MobileNumber,
                            g.Email,
                            g.GSTNumber,
                            g.BusinessNumber,
                            g.CurrencyCode,
                            g.PaymentGroupId,
                            g.DefaultDepotId,
                            g.DefaultPriceClassId,
                            g.PaymentMethodId,
                            g.IsActive,
                            g.IsOnHold,
                            g.ChargeGST,
                            g.Notes,
                            g.CreatedAt,
                            g.CreatedBy,
                            g.ModifiedAt,
                            g.ModifiedBy,
                            g.DeletedAt,
                            g.DeletedBy,
                            -- Lookup data
                            pg.GroupCode as PaymentGroupCode,
                            pg.GroupName as PaymentGroupName,
                            d.DepotName,
                            pc.ClassName as PriceClassName,
                            pm.MethodName as PaymentMethodName
                        FROM Growers g
                        LEFT JOIN PaymentGroups pg ON g.PaymentGroupId = pg.PaymentGroupId
                        LEFT JOIN Depots d ON g.DefaultDepotId = d.DepotId
                        LEFT JOIN PriceClasses pc ON g.DefaultPriceClassId = pc.PriceClassId
                        LEFT JOIN PaymentMethods pm ON g.PaymentMethodId = pm.PaymentMethodId
                        WHERE g.GrowerId = @GrowerId AND g.DeletedAt IS NULL";

                    var parameters = new { GrowerId = growerId };
                    var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, parameters);

                    if (result == null) return null!;

                    return MapDynamicToGrower(result);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowerByIdAsync for GrowerId {growerId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Grower> GetGrowerByNumberAsync(string growerNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            g.GrowerId,
                            g.GrowerNumber,
                            g.FullName,
                            g.CheckPayeeName,
                            g.Address,
                            g.City,
                            g.Province,
                            g.PostalCode,
                            g.PhoneNumber,
                            g.MobileNumber,
                            g.Email,
                            g.GSTNumber,
                            g.BusinessNumber,
                            g.CurrencyCode,
                            g.PaymentGroupId,
                            g.DefaultDepotId,
                            g.DefaultPriceClassId,
                            g.PaymentMethodId,
                            g.IsActive,
                            g.IsOnHold,
                            g.ChargeGST,
                            g.Notes,
                            g.CreatedAt,
                            g.CreatedBy,
                            g.ModifiedAt,
                            g.ModifiedBy,
                            g.DeletedAt,
                            g.DeletedBy
                        FROM Growers g
                        WHERE g.GrowerNumber = @GrowerNumber AND g.DeletedAt IS NULL";

                    var parameters = new { GrowerNumber = growerNumber };
                    var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, parameters);

                    if (result == null) return null!;

                    return MapDynamicToGrower(result);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowerByNumberAsync for GrowerNumber {growerNumber}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<Grower>> GetAllGrowersAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            g.GrowerId,
                            g.GrowerNumber,
                            g.FullName,
                            g.CheckPayeeName,
                            g.Address,
                            g.City,
                            g.Province,
                            g.PostalCode,
                            g.PhoneNumber,
                            g.MobileNumber,
                            g.Email,
                            g.GSTNumber,
                            g.BusinessNumber,
                            g.CurrencyCode,
                            g.PaymentGroupId,
                            g.DefaultDepotId,
                            g.DefaultPriceClassId,
                            g.PaymentMethodId,
                            g.IsActive,
                            g.IsOnHold,
                            g.ChargeGST,
                            g.Notes,
                            g.CreatedAt,
                            g.CreatedBy,
                            g.ModifiedAt,
                            g.ModifiedBy,
                            g.DeletedAt,
                            g.DeletedBy
                        FROM Growers g
                        WHERE g.DeletedAt IS NULL
                        ORDER BY g.FullName";

                    var results = await connection.QueryAsync<dynamic>(sql);
                    return results.Select(MapDynamicToGrower).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetAllGrowersAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<int> CreateGrowerAsync(Grower grower)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        INSERT INTO Growers (
                            GrowerNumber, FullName, CheckPayeeName, Address, City, Province, 
                            PostalCode, PhoneNumber, MobileNumber, Email, GSTNumber, BusinessNumber,
                            CurrencyCode, PaymentGroupId, DefaultDepotId, DefaultPriceClassId,
                            PaymentMethodId, IsActive, IsOnHold, ChargeGST, Notes,
                            CreatedAt, CreatedBy
                        )
                        VALUES (
                            @GrowerNumber, @FullName, @CheckPayeeName, @Address, @City, @Province,
                            @PostalCode, @PhoneNumber, @MobileNumber, @Email, @GSTNumber, @BusinessNumber,
                            @CurrencyCode, @PaymentGroupId, @DefaultDepotId, @DefaultPriceClassId,
                            @PaymentMethodId, @IsActive, @IsOnHold, @ChargeGST, @Notes,
                            GETDATE(), @CreatedBy
                        );
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    var currentUser = App.CurrentUser?.Username ?? "SYSTEM";
                    var parameters = new
                    {
                        grower.GrowerNumber,
                        grower.FullName,
                        grower.CheckPayeeName,
                        grower.Address,
                        grower.City,
                        grower.Province,
                        grower.PostalCode,
                        grower.PhoneNumber,
                        grower.MobileNumber,
                        grower.Email,
                        grower.GSTNumber,
                        grower.BusinessNumber,
                        grower.CurrencyCode,
                        grower.PaymentGroupId,
                        grower.DefaultDepotId,
                        grower.DefaultPriceClassId,
                        grower.PaymentMethodId,
                        grower.IsActive,
                        grower.IsOnHold,
                        grower.ChargeGST,
                        grower.Notes,
                        CreatedBy = currentUser
                    };

                    var growerId = await connection.QuerySingleAsync<int>(sql, parameters);
                    return growerId;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in CreateGrowerAsync for GrowerNumber {grower?.GrowerNumber}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> UpdateGrowerAsync(Grower grower)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Growers SET
                            GrowerNumber = @GrowerNumber,
                            FullName = @FullName,
                            CheckPayeeName = @CheckPayeeName,
                            Address = @Address,
                            City = @City,
                            Province = @Province,
                            PostalCode = @PostalCode,
                            PhoneNumber = @PhoneNumber,
                            MobileNumber = @MobileNumber,
                            Email = @Email,
                            GSTNumber = @GSTNumber,
                            BusinessNumber = @BusinessNumber,
                            CurrencyCode = @CurrencyCode,
                            PaymentGroupId = @PaymentGroupId,
                            DefaultDepotId = @DefaultDepotId,
                            DefaultPriceClassId = @DefaultPriceClassId,
                            PaymentMethodId = @PaymentMethodId,
                            IsActive = @IsActive,
                            IsOnHold = @IsOnHold,
                            ChargeGST = @ChargeGST,
                            Notes = @Notes,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE GrowerId = @GrowerId AND DeletedAt IS NULL";

                    var currentUser = App.CurrentUser?.Username ?? "SYSTEM";
                    var parameters = new
                    {
                        grower.GrowerId,
                        grower.GrowerNumber,
                        grower.FullName,
                        grower.CheckPayeeName,
                        grower.Address,
                        grower.City,
                        grower.Province,
                        grower.PostalCode,
                        grower.PhoneNumber,
                        grower.MobileNumber,
                        grower.Email,
                        grower.GSTNumber,
                        grower.BusinessNumber,
                        grower.CurrencyCode,
                        grower.PaymentGroupId,
                        grower.DefaultDepotId,
                        grower.DefaultPriceClassId,
                        grower.PaymentMethodId,
                        grower.IsActive,
                        grower.IsOnHold,
                        grower.ChargeGST,
                        grower.Notes,
                        ModifiedBy = currentUser
                    };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in UpdateGrowerAsync for GrowerId {grower?.GrowerId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> SaveGrowerAsync(Grower grower)
        {
            try
            {
                if (grower.GrowerId == 0)
                {
                    // Create new grower
                    var newGrowerId = await CreateGrowerAsync(grower);
                    return newGrowerId > 0;
                }
                else
                {
                    // Update existing grower
                    return await UpdateGrowerAsync(grower);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in SaveGrowerAsync for GrowerId {grower?.GrowerId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteGrowerAsync(int growerId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Growers SET
                            DeletedAt = GETDATE(),
                            DeletedBy = @DeletedBy
                        WHERE GrowerId = @GrowerId AND DeletedAt IS NULL";

                    var currentUser = App.CurrentUser?.Username ?? "SYSTEM";
                    var parameters = new { GrowerId = growerId, DeletedBy = currentUser };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in DeleteGrowerAsync for GrowerId {growerId}: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Search & Filter Operations

        public async Task<List<GrowerSearchResult>> SearchGrowersAsync(string searchTerm)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT
                            g.GrowerId,
                            g.GrowerNumber,
                            g.FullName as GrowerName,
                            g.CheckPayeeName as ChequeName,
                            g.City,
                            g.Province,
                            g.PhoneNumber as Phone,
                            g.Email,
                            g.IsActive,
                            g.IsOnHold,
                            pg.GroupCode as PaymentGroupCode
                        FROM Growers g
                        LEFT JOIN PaymentGroups pg ON g.PaymentGroupId = pg.PaymentGroupId
                        WHERE g.DeletedAt IS NULL
                        AND (
                            g.FullName LIKE @SearchTerm
                            OR g.CheckPayeeName LIKE @SearchTerm
                            OR g.City LIKE @SearchTerm
                            OR g.PhoneNumber LIKE @SearchTerm
                            OR g.MobileNumber LIKE @SearchTerm
                            OR g.GrowerNumber LIKE @SearchTerm
                            OR g.Email LIKE @SearchTerm
                        )
                        ORDER BY g.FullName";

                    var parameters = new { SearchTerm = $"%{searchTerm}%" };
                    return (await connection.QueryAsync<GrowerSearchResult>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in SearchGrowersAsync for term '{searchTerm}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<GrowerSearchResult>> GetAllGrowersForListAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT
                            g.GrowerId,
                            g.GrowerNumber,
                            g.FullName as GrowerName,
                            g.CheckPayeeName as ChequeName,
                            g.City,
                            g.Province,
                            g.PhoneNumber as Phone,
                            g.Email,
                            g.IsActive,
                            g.IsOnHold,
                            pg.GroupCode as PaymentGroupCode
                        FROM Growers g
                        LEFT JOIN PaymentGroups pg ON g.PaymentGroupId = pg.PaymentGroupId
                        WHERE g.DeletedAt IS NULL
                        ORDER BY g.FullName";

                    return (await connection.QueryAsync<GrowerSearchResult>(sql)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetAllGrowersForListAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<GrowerSearchResult>> GetGrowersByProvinceAsync(string province)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT
                            g.GrowerId,
                            g.GrowerNumber,
                            g.FullName as GrowerName,
                            g.CheckPayeeName as ChequeName,
                            g.City,
                            g.Province,
                            g.PhoneNumber as Phone,
                            g.Email,
                            g.IsActive,
                            g.IsOnHold,
                            pg.GroupCode as PaymentGroupCode
                        FROM Growers g
                        LEFT JOIN PaymentGroups pg ON g.PaymentGroupId = pg.PaymentGroupId
                        WHERE g.DeletedAt IS NULL AND g.Province = @Province
                        ORDER BY g.FullName";

                    var parameters = new { Province = province };
                    return (await connection.QueryAsync<GrowerSearchResult>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowersByProvinceAsync for province '{province}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<GrowerSearchResult>> GetGrowersByPaymentGroupAsync(int paymentGroupId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT
                            g.GrowerId,
                            g.GrowerNumber,
                            g.FullName as GrowerName,
                            g.CheckPayeeName as ChequeName,
                            g.City,
                            g.Province,
                            g.PhoneNumber as Phone,
                            g.Email,
                            g.IsActive,
                            g.IsOnHold,
                            pg.GroupCode as PaymentGroupCode
                        FROM Growers g
                        LEFT JOIN PaymentGroups pg ON g.PaymentGroupId = pg.PaymentGroupId
                        WHERE g.DeletedAt IS NULL AND g.PaymentGroupId = @PaymentGroupId
                        ORDER BY g.FullName";

                    var parameters = new { PaymentGroupId = paymentGroupId };
                    return (await connection.QueryAsync<GrowerSearchResult>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowersByPaymentGroupAsync for PaymentGroupId {paymentGroupId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<GrowerSearchResult>> GetGrowersByStatusAsync(bool? isActive = null, bool? isOnHold = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var whereClause = "WHERE g.DeletedAt IS NULL";
                    var parameters = new Dictionary<string, object>();

                    if (isActive.HasValue)
                    {
                        whereClause += " AND g.IsActive = @IsActive";
                        parameters.Add("IsActive", isActive.Value);
                    }

                    if (isOnHold.HasValue)
                    {
                        whereClause += " AND g.IsOnHold = @IsOnHold";
                        parameters.Add("IsOnHold", isOnHold.Value);
                    }

                    var sql = $@"
                        SELECT
                            g.GrowerId,
                            g.GrowerNumber,
                            g.FullName as GrowerName,
                            g.CheckPayeeName as ChequeName,
                            g.City,
                            g.Province,
                            g.PhoneNumber as Phone,
                            g.Email,
                            g.IsActive,
                            g.IsOnHold,
                            pg.GroupCode as PaymentGroupCode
                        FROM Growers g
                        LEFT JOIN PaymentGroups pg ON g.PaymentGroupId = pg.PaymentGroupId
                        {whereClause}
                        ORDER BY g.FullName";

                    return (await connection.QueryAsync<GrowerSearchResult>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowersByStatusAsync: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Validation Operations

        public async Task<bool> IsGrowerNumberUniqueAsync(string growerNumber, int? excludeGrowerId = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT COUNT(*) FROM Growers WHERE GrowerNumber = @GrowerNumber AND DeletedAt IS NULL";
                    object parameters;

                    if (excludeGrowerId.HasValue)
                    {
                        sql += " AND GrowerId != @ExcludeGrowerId";
                        parameters = new { GrowerNumber = growerNumber, ExcludeGrowerId = excludeGrowerId.Value };
                    }
                    else
                    {
                        parameters = new { GrowerNumber = growerNumber };
                    }

                    var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
                    return count == 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in IsGrowerNumberUniqueAsync for GrowerNumber '{growerNumber}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> GrowerExistsAsync(int growerId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT COUNT(*) FROM Growers WHERE GrowerId = @GrowerId AND DeletedAt IS NULL";
                    var parameters = new { GrowerId = growerId };

                    var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GrowerExistsAsync for GrowerId {growerId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Dictionary<string, string>> ValidateGrowerAsync(Grower grower)
        {
            var errors = new Dictionary<string, string>();

            // Required field validation
            if (string.IsNullOrWhiteSpace(grower.GrowerNumber))
                errors["GrowerNumber"] = "Grower Number is required.";
            else if (grower.GrowerNumber.Length > 20)
                errors["GrowerNumber"] = "Grower Number cannot exceed 20 characters.";
            else if (grower.GrowerNumber.Length < 1)
                errors["GrowerNumber"] = "Grower Number must be at least 1 character.";

            if (string.IsNullOrWhiteSpace(grower.FullName))
                errors["FullName"] = "Full Name is required.";
            else if (grower.FullName.Length > 100)
                errors["FullName"] = "Full Name cannot exceed 100 characters.";
            else if (grower.FullName.Length < 2)
                errors["FullName"] = "Full Name must be at least 2 characters.";

            // Optional field length validations
            if (!string.IsNullOrWhiteSpace(grower.CheckPayeeName) && grower.CheckPayeeName.Length > 100)
                errors["CheckPayeeName"] = "Check Payee Name cannot exceed 100 characters.";

            if (!string.IsNullOrWhiteSpace(grower.Address) && grower.Address.Length > 200)
                errors["Address"] = "Address cannot exceed 200 characters.";

            if (!string.IsNullOrWhiteSpace(grower.City) && grower.City.Length > 50)
                errors["City"] = "City cannot exceed 50 characters.";

            if (!string.IsNullOrWhiteSpace(grower.Province) && grower.Province.Length > 2)
                errors["Province"] = "Province cannot exceed 2 characters.";

            if (!string.IsNullOrWhiteSpace(grower.PostalCode) && grower.PostalCode.Length > 10)
                errors["PostalCode"] = "Postal Code cannot exceed 10 characters.";

            if (!string.IsNullOrWhiteSpace(grower.PhoneNumber) && grower.PhoneNumber.Length > 20)
                errors["PhoneNumber"] = "Phone Number cannot exceed 20 characters.";

            if (!string.IsNullOrWhiteSpace(grower.MobileNumber) && grower.MobileNumber.Length > 20)
                errors["MobileNumber"] = "Mobile Number cannot exceed 20 characters.";

            if (!string.IsNullOrWhiteSpace(grower.Email) && grower.Email.Length > 100)
                errors["Email"] = "Email cannot exceed 100 characters.";

            if (!string.IsNullOrWhiteSpace(grower.GSTNumber) && grower.GSTNumber.Length > 20)
                errors["GSTNumber"] = "GST Number cannot exceed 20 characters.";

            if (!string.IsNullOrWhiteSpace(grower.BusinessNumber) && grower.BusinessNumber.Length > 20)
                errors["BusinessNumber"] = "Business Number cannot exceed 20 characters.";

            if (!string.IsNullOrWhiteSpace(grower.Notes) && grower.Notes.Length > 2000)
                errors["Notes"] = "Notes cannot exceed 2000 characters.";

            // Email format validation
            if (!string.IsNullOrWhiteSpace(grower.Email) && !IsValidEmail(grower.Email))
                errors["Email"] = "Invalid email format. Please enter a valid email address.";

            // Phone number format validation
            if (!string.IsNullOrWhiteSpace(grower.PhoneNumber) && !IsValidPhoneNumber(grower.PhoneNumber))
                errors["PhoneNumber"] = "Invalid phone number format. Use format: (123) 456-7890 or 123-456-7890";

            if (!string.IsNullOrWhiteSpace(grower.MobileNumber) && !IsValidPhoneNumber(grower.MobileNumber))
                errors["MobileNumber"] = "Invalid mobile number format. Use format: (123) 456-7890 or 123-456-7890";

            // Postal code validation (Canadian format)
            if (!string.IsNullOrWhiteSpace(grower.PostalCode) && !IsValidCanadianPostalCode(grower.PostalCode))
                errors["PostalCode"] = "Invalid postal code format. Use format: A1A 1A1";

            // Province validation (Canadian provinces)
            if (!string.IsNullOrWhiteSpace(grower.Province) && !IsValidCanadianProvince(grower.Province))
                errors["Province"] = "Invalid province code. Use valid Canadian province codes (AB, BC, MB, etc.)";

            // Currency validation
            if (!string.IsNullOrWhiteSpace(grower.CurrencyCode) && 
                !new[] { "CAD", "USD" }.Contains(grower.CurrencyCode.ToUpper()))
                errors["CurrencyCode"] = "Currency Code must be CAD or USD.";

            // GST Number validation (Canadian format)
            if (!string.IsNullOrWhiteSpace(grower.GSTNumber) && !IsValidGSTNumber(grower.GSTNumber))
                errors["GSTNumber"] = "Invalid GST number format. Use format: 123456789RT0001";

            // Business Number validation (Canadian format)
            if (!string.IsNullOrWhiteSpace(grower.BusinessNumber) && !IsValidBusinessNumber(grower.BusinessNumber))
                errors["BusinessNumber"] = "Invalid business number format. Use format: 123456789";

            // Required foreign key validations
            if (grower.PaymentGroupId <= 0)
                errors["PaymentGroupId"] = "Payment Group is required.";

            if (grower.DefaultDepotId <= 0)
                errors["DefaultDepotId"] = "Default Depot is required.";

            if (grower.DefaultPriceClassId <= 0)
                errors["DefaultPriceClassId"] = "Default Price Class is required.";

            // Unique grower number validation
            if (!string.IsNullOrWhiteSpace(grower.GrowerNumber))
            {
                var excludeId = grower.GrowerId > 0 ? grower.GrowerId : (int?)null;
                var isUnique = await IsGrowerNumberUniqueAsync(grower.GrowerNumber, excludeId);
                if (!isUnique)
                    errors["GrowerNumber"] = "Grower Number must be unique. This number is already in use.";

                // Check for invalid characters in grower number
                if (grower.GrowerNumber.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_'))
                    errors["GrowerNumber"] = "Grower Number can only contain letters, numbers, hyphens, and underscores.";
            }

            return errors;
        }

        #endregion

        #region Statistics & History Operations

        public async Task<GrowerStatistics> GetGrowerStatisticsAsync(int growerId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            @GrowerId as GrowerId,
                            ISNULL((SELECT COUNT(*) FROM Receipts WHERE GrowerId = @GrowerId), 0) as TotalReceipts,
                            ISNULL((SELECT SUM(TotalAmount) FROM Receipts WHERE GrowerId = @GrowerId), 0) as TotalReceiptsValue,
                            ISNULL((SELECT COUNT(*) FROM Payments WHERE GrowerId = @GrowerId), 0) as TotalPayments,
                            ISNULL((SELECT SUM(Amount) FROM Payments WHERE GrowerId = @GrowerId), 0) as TotalPaymentsValue,
                            (SELECT MAX(ReceiptDate) FROM Receipts WHERE GrowerId = @GrowerId) as LastReceiptDate,
                            (SELECT MAX(PaymentDate) FROM Payments WHERE GrowerId = @GrowerId) as LastPaymentDate,
                            ISNULL((SELECT COUNT(*) FROM Receipts WHERE GrowerId = @GrowerId AND YEAR(ReceiptDate) = YEAR(GETDATE())), 0) as CurrentYearReceipts,
                            ISNULL((SELECT SUM(TotalAmount) FROM Receipts WHERE GrowerId = @GrowerId AND YEAR(ReceiptDate) = YEAR(GETDATE())), 0) as CurrentYearValue";

                    var parameters = new { GrowerId = growerId };
                    return await connection.QueryFirstOrDefaultAsync<GrowerStatistics>(sql, parameters) ?? new GrowerStatistics { GrowerId = growerId };
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowerStatisticsAsync for GrowerId {growerId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<Receipt>> GetGrowerRecentReceiptsAsync(int growerId, int count = 10)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT TOP (@Count)
                            ReceiptId, ReceiptNumber, GrowerId, ReceiptDate, 
                            TotalAmount, Status, CreatedAt
                        FROM Receipts
                        WHERE GrowerId = @GrowerId
                        ORDER BY ReceiptDate DESC, ReceiptId DESC";

                    var parameters = new { GrowerId = growerId, Count = count };
                    return (await connection.QueryAsync<Receipt>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowerRecentReceiptsAsync for GrowerId {growerId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<Payment>> GetGrowerRecentPaymentsAsync(int growerId, int count = 10)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT TOP (@Count)
                            PaymentId, PaymentBatchId, GrowerId, Amount, 
                            PaymentDate, PaymentTypeId, Status, CreatedAt
                        FROM Payments
                        WHERE GrowerId = @GrowerId
                        ORDER BY PaymentDate DESC, PaymentId DESC";

                    var parameters = new { GrowerId = growerId, Count = count };
                    return (await connection.QueryAsync<Payment>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowerRecentPaymentsAsync for GrowerId {growerId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<Receipt>> GetGrowerReceiptsAsync(int growerId, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            ReceiptId, ReceiptNumber, GrowerId, ReceiptDate, 
                            TotalAmount, Status, CreatedAt
                        FROM Receipts
                        WHERE GrowerId = @GrowerId";

                    var parameters = new Dictionary<string, object> { { "GrowerId", growerId } };

                    if (fromDate.HasValue)
                    {
                        sql += " AND ReceiptDate >= @FromDate";
                        parameters.Add("FromDate", fromDate.Value);
                    }

                    if (toDate.HasValue)
                    {
                        sql += " AND ReceiptDate <= @ToDate";
                        parameters.Add("ToDate", toDate.Value);
                    }

                    sql += " ORDER BY ReceiptDate DESC, ReceiptId DESC";

                    return (await connection.QueryAsync<Receipt>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowerReceiptsAsync for GrowerId {growerId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<Payment>> GetGrowerPaymentsAsync(int growerId, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            PaymentId, PaymentBatchId, GrowerId, Amount, 
                            PaymentDate, PaymentTypeId, Status, CreatedAt
                        FROM Payments
                        WHERE GrowerId = @GrowerId";

                    var parameters = new Dictionary<string, object> { { "GrowerId", growerId } };

                    if (fromDate.HasValue)
                    {
                        sql += " AND PaymentDate >= @FromDate";
                        parameters.Add("FromDate", fromDate.Value);
                    }

                    if (toDate.HasValue)
                    {
                        sql += " AND PaymentDate <= @ToDate";
                        parameters.Add("ToDate", toDate.Value);
                    }

                    sql += " ORDER BY PaymentDate DESC, PaymentId DESC";

                    return (await connection.QueryAsync<Payment>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowerPaymentsAsync for GrowerId {growerId}: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Dashboard & Reporting Operations

        public async Task<int> GetTotalGrowersCountAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT COUNT(*) FROM Growers WHERE DeletedAt IS NULL";
                    return await connection.ExecuteScalarAsync<int>(sql);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetTotalGrowersCountAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<int> GetActiveGrowersCountAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT COUNT(*) FROM Growers WHERE IsActive = 1 AND DeletedAt IS NULL";
                    return await connection.ExecuteScalarAsync<int>(sql);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetActiveGrowersCountAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<int> GetOnHoldGrowersCountAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT COUNT(*) FROM Growers WHERE IsOnHold = 1 AND DeletedAt IS NULL";
                    return await connection.ExecuteScalarAsync<int>(sql);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetOnHoldGrowersCountAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<int> GetInactiveGrowersCountAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT COUNT(*) FROM Growers WHERE IsActive = 0 AND DeletedAt IS NULL";
                    return await connection.ExecuteScalarAsync<int>(sql);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetInactiveGrowersCountAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<string>> GetUniqueProvincesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT DISTINCT Province
                        FROM Growers
                        WHERE Province IS NOT NULL 
                        AND Province <> '' 
                        AND DeletedAt IS NULL
                        ORDER BY Province";

                    var provinces = await connection.QueryAsync<string>(sql);
                    return provinces.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetUniqueProvincesAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetGrowerCountsByProvinceAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT Province, COUNT(*) as GrowerCount
                        FROM Growers
                        WHERE Province IS NOT NULL 
                        AND Province <> '' 
                        AND DeletedAt IS NULL
                        GROUP BY Province
                        ORDER BY Province";

                    var results = await connection.QueryAsync<dynamic>(sql);
                    return results.ToDictionary(x => (string)x.Province, x => (int)x.GrowerCount);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowerCountsByProvinceAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Dictionary<int, int>> GetGrowerCountsByPaymentGroupAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT PaymentGroupId, COUNT(*) as GrowerCount
                        FROM Growers
                        WHERE DeletedAt IS NULL
                        GROUP BY PaymentGroupId
                        ORDER BY PaymentGroupId";

                    var results = await connection.QueryAsync<dynamic>(sql);
                    return results.ToDictionary(x => (int)x.PaymentGroupId, x => (int)x.GrowerCount);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowerCountsByPaymentGroupAsync: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Lookup & Reference Data

        public async Task<List<GrowerInfo>> GetAllGrowersBasicInfoAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT GrowerId, GrowerNumber, FullName as Name
                        FROM Growers
                        WHERE DeletedAt IS NULL
                        ORDER BY GrowerNumber";

                    var growers = await connection.QueryAsync<GrowerInfo>(sql);
                    return growers.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetAllGrowersBasicInfoAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<GrowerInfo>> GetOnHoldGrowersAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT GrowerId, GrowerNumber, FullName as Name
                        FROM Growers
                        WHERE IsOnHold = 1 AND DeletedAt IS NULL
                        ORDER BY FullName";

                    var growers = await connection.QueryAsync<GrowerInfo>(sql);
                    return growers.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetOnHoldGrowersAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<GrowerInfo>> GetActiveGrowersAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT GrowerId, GrowerNumber, FullName as Name
                        FROM Growers
                        WHERE IsActive = 1 AND DeletedAt IS NULL
                        ORDER BY FullName";

                    var growers = await connection.QueryAsync<GrowerInfo>(sql);
                    return growers.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetActiveGrowersAsync: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private static Grower MapDynamicToGrower(dynamic result)
        {
            return new Grower
            {
                GrowerId = result.GrowerId,
                GrowerNumber = result.GrowerNumber ?? string.Empty,
                FullName = result.FullName ?? string.Empty,
                CheckPayeeName = result.CheckPayeeName ?? string.Empty,
                Address = result.Address ?? string.Empty,
                City = result.City ?? string.Empty,
                Province = result.Province ?? string.Empty,
                PostalCode = result.PostalCode ?? string.Empty,
                PhoneNumber = result.PhoneNumber ?? string.Empty,
                MobileNumber = result.MobileNumber ?? string.Empty,
                Email = result.Email ?? string.Empty,
                GSTNumber = result.GSTNumber ?? string.Empty,
                BusinessNumber = result.BusinessNumber ?? string.Empty,
                CurrencyCode = result.CurrencyCode ?? "CAD",
                PaymentGroupId = result.PaymentGroupId,
                DefaultDepotId = result.DefaultDepotId,
                DefaultPriceClassId = result.DefaultPriceClassId,
                PaymentMethodId = result.PaymentMethodId,
                IsActive = result.IsActive,
                IsOnHold = result.IsOnHold,
                ChargeGST = result.ChargeGST,
                Notes = result.Notes ?? string.Empty,
                CreatedAt = result.CreatedAt,
                CreatedBy = result.CreatedBy ?? string.Empty,
                ModifiedAt = result.ModifiedAt,
                ModifiedBy = result.ModifiedBy ?? string.Empty,
                DeletedAt = result.DeletedAt,
                DeletedBy = result.DeletedBy ?? string.Empty
            };
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return true;

            // Remove all non-digit characters
            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
            
            // Check if we have 10 digits (North American format)
            if (digitsOnly.Length == 10)
                return true;

            // Check if we have 11 digits starting with 1 (North American format with country code)
            if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
                return true;

            return false;
        }

        private static bool IsValidCanadianPostalCode(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
                return true;

            // Canadian postal code pattern: A1A 1A1
            var pattern = @"^[A-Za-z]\d[A-Za-z] \d[A-Za-z]\d$";
            return System.Text.RegularExpressions.Regex.IsMatch(postalCode, pattern);
        }

        private static bool IsValidCanadianProvince(string province)
        {
            if (string.IsNullOrWhiteSpace(province))
                return true;

            var validProvinces = new[]
            {
                "AB", "BC", "MB", "NB", "NL", "NS", "NT", "NU", "ON", "PE", "QC", "SK", "YT"
            };

            return validProvinces.Contains(province.ToUpper());
        }

        private static bool IsValidGSTNumber(string gstNumber)
        {
            if (string.IsNullOrWhiteSpace(gstNumber))
                return true;

            // Canadian GST number pattern: 9 digits + RT + 4 digits
            var pattern = @"^\d{9}RT\d{4}$";
            return System.Text.RegularExpressions.Regex.IsMatch(gstNumber.ToUpper(), pattern);
        }

        private static bool IsValidBusinessNumber(string businessNumber)
        {
            if (string.IsNullOrWhiteSpace(businessNumber))
                return true;

            // Canadian business number: 9 digits
            var pattern = @"^\d{9}$";
            return System.Text.RegularExpressions.Regex.IsMatch(businessNumber, pattern);
        }

        #endregion
    }
}