using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Services
{
    public class GrowerService : BaseDatabaseService, IGrowerService
    {
        public async Task<List<GrowerSearchResult>> SearchGrowersAsync(string searchTerm)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            NUMBER as GrowerNumber,
                            NAME as GrowerName,
                            CHEQNAME as ChequeName,
                            CITY as City,
                            PHONE as Phone
                        FROM GROWER 
                        WHERE NAME LIKE @SearchTerm 
                           OR CHEQNAME LIKE @SearchTerm 
                           OR CITY LIKE @SearchTerm
                        ORDER BY NAME";

                    var parameters = new { SearchTerm = $"%{searchTerm}%" };
                    return (await connection.QueryAsync<GrowerSearchResult>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SearchGrowersAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<Grower> GetGrowerByNumberAsync(decimal growerNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            NUMBER as GrowerNumber,
                            CHEQNAME as ChequeName,
                            NAME as GrowerName,
                            STREET as Address,
                            CITY as City,
                            PROV as Prov,
                            PCODE as Postal,
                            PHONE as Phone,
                            ACRES as Acres,
                            NOTES as Notes,
                            CONTRACT as Contract,
                            CURRENCY as Currency,
                            CONTLIM as ContractLimit,
                            PAYGRP as PayGroup,
                            ONHOLD as OnHold,
                            PHONE2 as PhoneAdditional1,
                            STREET2 as OtherNames,
                            ALT_PHONE1 as PhoneAdditional2,
                            LY_FRESH as LYFresh,
                            LY_OTHER as LYOther,
                            CERTIFIED as Certified,
                            CHG_GST as ChargeGST
                        FROM GROWER 
                        WHERE NUMBER = @GrowerNumber";

                    var parameters = new { GrowerNumber = growerNumber };
                    return await connection.QueryFirstOrDefaultAsync<Grower>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetGrowerByNumberAsync: {ex.Message}");
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
                        MERGE INTO GROWER AS target
                        USING (SELECT @GrowerNumber AS NUMBER) AS source
                        ON (target.NUMBER = source.NUMBER)
                        WHEN MATCHED THEN
                            UPDATE SET
                                CHEQNAME = @ChequeName,
                                NAME = @GrowerName,
                                STREET = @Address,
                                CITY = @City,
                                PROV = @Prov,
                                PCODE = @Postal,
                                PHONE = @Phone,
                                ACRES = @Acres,
                                NOTES = @Notes,
                                CONTRACT = @Contract,
                                CURRENCY = @Currency,
                                CONTLIM = @ContractLimit,
                                PAYGRP = @PayGroup,
                                ONHOLD = @OnHold,
                                PHONE2 = @PhoneAdditional1,
                                STREET2 = @OtherNames,
                                ALT_PHONE1 = @PhoneAdditional2,
                                LY_FRESH = @LYFresh,
                                LY_OTHER = @LYOther,
                                CERTIFIED = @Certified,
                                CHG_GST = @ChargeGST,
                                QED_DATE = GETDATE(),
                                QED_TIME = CONVERT(varchar(8), GETDATE(), 108),
                                QED_OP = SYSTEM_USER
                        WHEN NOT MATCHED THEN
                            INSERT (
                                NUMBER, CHEQNAME, NAME, STREET, CITY, PROV, PCODE, PHONE,
                                ACRES, NOTES, CONTRACT, CURRENCY, CONTLIM, PAYGRP, ONHOLD,
                                PHONE2, STREET2, ALT_PHONE1, LY_FRESH, LY_OTHER, CERTIFIED,
                                CHG_GST, QADD_DATE, QADD_TIME, QADD_OP
                            )
                            VALUES (
                                @GrowerNumber, @ChequeName, @GrowerName, @Address, @City,
                                @Prov, @Postal, @Phone, @Acres, @Notes, @Contract, @Currency,
                                @ContractLimit, @PayGroup, @OnHold, @PhoneAdditional1,
                                @OtherNames, @PhoneAdditional2, @LYFresh, @LYOther,
                                @Certified, @ChargeGST, GETDATE(),
                                CONVERT(varchar(8), GETDATE(), 108), SYSTEM_USER
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
                        grower.ChargeGST
                    };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveGrowerAsync: {ex.Message}");
                return false;
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
                            NUMBER as GrowerNumber,
                            NAME as GrowerName,
                            CHEQNAME as ChequeName,
                            CITY as City,
                            PHONE as Phone
                        FROM GROWER 
                        ORDER BY NAME";

                    return (await connection.QueryAsync<GrowerSearchResult>(sql)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAllGrowersAsync: {ex.Message}");
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
                        SELECT DISTINCT PROV 
                        FROM GROWER 
                        WHERE PROV IS NOT NULL 
                        AND PROV <> ''
                        ORDER BY PROV";

                    var provinces = await connection.QueryAsync<string>(sql);
                    return provinces.ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetUniqueProvincesAsync: {ex.Message}");
                throw;
            }
        }
    }
} 