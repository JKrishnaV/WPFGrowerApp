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
using WPFGrowerApp.Infrastructure.Logging; // Ensure Logger namespace is included

namespace WPFGrowerApp.DataAccess.Services
{
    public class GrowerService : BaseDatabaseService, IGrowerService
    {
        public GrowerService() : base() { }

        public async Task<Grower> GetGrowerByIdAsync(int growerId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                    SELECT
                        g.GrowerNumber,
                        g.CheckPayeeName as ChequeName,
                        g.FullName as GrowerName,
                        g.Address,
                        g.City,
                        g.Province as Prov,
                        g.PostalCode as Postal,
                        g.PhoneNumber as Phone,
                        0 as Acres,
                        g.Notes,
                        '' as Contract,
                        LEFT(g.CurrencyCode, 1) as Currency,
                        0 as ContractLimit,
                        ISNULL(pg.GroupCode, 'STD') as PayGroup,
                        g.IsOnHold as OnHold,
                        g.MobileNumber as PhoneAdditional1,
                        '' as OtherNames,
                        '' as PhoneAdditional2,
                        0 as LYFresh,
                        0 as LYOther,
                        '' as Certified,
                        g.ChargeGST,
                        g.DefaultPriceClassId
                    FROM Growers g
                    LEFT JOIN PaymentGroups pg ON g.PaymentGroupId = pg.PaymentGroupId
                    WHERE g.GrowerId = @GrowerId";

                    var parameters = new { GrowerId = growerId };
                    var grower = await connection.QueryFirstOrDefaultAsync<Grower>(sql, parameters);

                    if (grower != null)
                    {
                        if (grower.GrowerName != null) grower.GrowerName = grower.GrowerName.Trim();
                        if (grower.ChequeName != null) grower.ChequeName = grower.ChequeName.Trim();
                        if (grower.Address != null) grower.Address = grower.Address.Trim();
                        if (grower.City != null) grower.City = grower.City.Trim();
                        if (grower.Prov != null) grower.Prov = grower.Prov.Trim();
                        if (grower.Postal != null) grower.Postal = grower.Postal.Trim();
                        if (grower.Phone != null) grower.Phone = grower.Phone.Trim();
                        if (grower.Notes != null) grower.Notes = grower.Notes.Trim();
                        if (grower.PhoneAdditional1 != null) grower.PhoneAdditional1 = grower.PhoneAdditional1.Trim();
                        if (grower.OtherNames != null) grower.OtherNames = grower.OtherNames.Trim();
                        if (grower.PhoneAdditional2 != null) grower.PhoneAdditional2 = grower.PhoneAdditional2.Trim();
                    }
                    return grower;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowerByIdAsync for GrowerId {growerId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<GrowerSearchResult>> SearchGrowersAsync(string searchTerm)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                    SELECT
                        GrowerId,
                        GrowerNumber,
                        FullName as GrowerName,
                        CheckPayeeName as ChequeName,
                        City,
                        PhoneNumber as Phone
                    FROM Growers
                    WHERE FullName LIKE @SearchTerm
                       OR CheckPayeeName LIKE @SearchTerm
                       OR City LIKE @SearchTerm
                       OR PhoneNumber LIKE @SearchTerm
                       OR GrowerNumber LIKE @SearchTerm
                    ORDER BY FullName";
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

        public async Task<Grower> GetGrowerByNumberAsync(string growerNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                    SELECT
                        g.GrowerNumber,
                        g.CheckPayeeName as ChequeName,
                        g.FullName as GrowerName,
                        g.Address,
                        g.City,
                        g.Province as Prov,
                        g.PostalCode as Postal,
                        g.PhoneNumber as Phone,
                        0 as Acres,
                        g.Notes,
                        '' as Contract,
                        LEFT(g.CurrencyCode, 1) as Currency,
                        0 as ContractLimit,
                        ISNULL(pg.GroupCode, 'STD') as PayGroup,
                        g.IsOnHold as OnHold,
                        g.MobileNumber as PhoneAdditional1,
                        '' as OtherNames,
                        '' as PhoneAdditional2,
                        0 as LYFresh,
                        0 as LYOther,
                        '' as Certified,
                        g.ChargeGST
                    FROM Growers g
                    LEFT JOIN PaymentGroups pg ON g.PaymentGroupId = pg.PaymentGroupId
                    WHERE g.GrowerNumber = @GrowerNumber";
                    var parameters = new { GrowerNumber = growerNumber };
                    var grower = await connection.QueryFirstOrDefaultAsync<Grower>(sql, parameters);
                    if (grower != null)
                    {
                        if (grower.GrowerName != null) grower.GrowerName = grower.GrowerName.Trim();
                        if (grower.ChequeName != null) grower.ChequeName = grower.ChequeName.Trim();
                        if (grower.Address != null) grower.Address = grower.Address.Trim();
                        if (grower.City != null) grower.City = grower.City.Trim();
                        if (grower.Prov != null) grower.Prov = grower.Prov.Trim();
                        if (grower.Postal != null) grower.Postal = grower.Postal.Trim();
                        if (grower.Phone != null) grower.Phone = grower.Phone.Trim();
                        if (grower.Notes != null) grower.Notes = grower.Notes.Trim();
                        if (grower.PhoneAdditional1 != null) grower.PhoneAdditional1 = grower.PhoneAdditional1.Trim();
                        if (grower.OtherNames != null) grower.OtherNames = grower.OtherNames.Trim();
                        if (grower.PhoneAdditional2 != null) grower.PhoneAdditional2 = grower.PhoneAdditional2.Trim();
                    }
                    return grower;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetGrowerByNumberAsync for GrowerNumber {growerNumber}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> SaveGrowerAsync(Grower grower)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                    MERGE INTO Growers AS target
                    USING (SELECT @GrowerNumber AS GrowerNumber) AS source
                    ON (target.GrowerNumber = source.GrowerNumber)
                    WHEN MATCHED THEN
                        UPDATE SET
                            CheckPayeeName = @ChequeName,
                            FullName = @GrowerName,
                            Address = @Address,
                            City = @City,
                            Province = @Prov,
                            PostalCode = @Postal,
                            PhoneNumber = @Phone,
                            Notes = @Notes,
                            CurrencyCode = CASE WHEN @Currency = 'C' THEN 'CAD' WHEN @Currency = 'U' THEN 'USD' ELSE 'CAD' END,
                            PaymentGroupId = (SELECT PaymentGroupId FROM PaymentGroups WHERE GroupCode = @PayGroup),
                            IsOnHold = @OnHold,
                            MobileNumber = @PhoneAdditional1,
                            ChargeGST = @ChargeGST,
                            DefaultPriceClassId = @DefaultPriceClassId,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = SYSTEM_USER
                    WHEN NOT MATCHED THEN
                        INSERT (
                            GrowerNumber, CheckPayeeName, FullName, Address, City, Province, PostalCode, PhoneNumber,
                            Notes, CurrencyCode, PaymentGroupId, IsOnHold, MobileNumber, ChargeGST,
                            DefaultPriceClassId, IsActive, CreatedAt, CreatedBy, DefaultDepotId
                        )
                        VALUES (
                            @GrowerNumber, @ChequeName, @GrowerName, @Address, @City,
                            @Prov, @Postal, @Phone, @Notes,
                            CASE WHEN @Currency = 'C' THEN 'CAD' WHEN @Currency = 'U' THEN 'USD' ELSE 'CAD' END,
                            (SELECT PaymentGroupId FROM PaymentGroups WHERE GroupCode = @PayGroup),
                            @OnHold, @PhoneAdditional1, @ChargeGST,
                            @DefaultPriceClassId, 1, GETDATE(), SYSTEM_USER, 1
                        );";
                    var parameters = new
                    {
                        grower.GrowerNumber,
                        grower.ChequeName,
                        grower.GrowerName,
                        grower.Address,
                        grower.City,
                        grower.Prov,
                        grower.Postal,
                        grower.Phone,
                        grower.Acres,
                        grower.Notes,
                        grower.Contract,
                        grower.Currency,
                        grower.ContractLimit,
                        grower.PayGroup,
                        grower.OnHold,
                        grower.PhoneAdditional1,
                        grower.OtherNames,
                        grower.PhoneAdditional2,
                        grower.LYFresh,
                        grower.LYOther,
                        grower.Certified,
                        grower.ChargeGST,
                        grower.DefaultPriceClassId
                    };
                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in SaveGrowerAsync for GrowerNumber {grower?.GrowerNumber}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<GrowerSearchResult>> GetAllGrowersAsync()
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
                        g.PhoneNumber as Phone,
                        g.Province,
                        0 as Acres,
                        g.Notes,
                        ISNULL(pg.GroupCode, 'STD') as PayGroup,
                        g.MobileNumber as Phone2,
                        g.IsOnHold
                    FROM Growers g
                    LEFT JOIN PaymentGroups pg ON g.PaymentGroupId = pg.PaymentGroupId
                    ORDER BY g.FullName";
                    return (await connection.QueryAsync<GrowerSearchResult>(sql)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetAllGrowersAsync: {ex.Message}", ex);
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

        public async Task<List<GrowerInfo>> GetAllGrowersBasicInfoAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                    SELECT
                        GrowerNumber, 
                        FullName as Name
                    FROM Growers
                    ORDER BY GrowerNumber";
                    var growers = await connection.QueryAsync<GrowerInfo>(sql);
                    foreach (var grower in growers)
                    {
                        if (grower.Name != null)
                            grower.Name = grower.Name.Trim();
                    }
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
                    SELECT
                        GrowerNumber, 
                        FullName as Name
                    FROM Growers
                    WHERE IsOnHold = 1
                    ORDER BY FullName";
                    var growers = await connection.QueryAsync<GrowerInfo>(sql);
                    foreach (var grower in growers)
                    {
                        if (grower.Name != null)
                            grower.Name = grower.Name.Trim();
                    }
                    return growers.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetOnHoldGrowersAsync: {ex.Message}", ex);
                throw;
            }
        }
    }
}
