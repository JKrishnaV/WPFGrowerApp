using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WPFGrowerApp.Models;
using WPFGrowerApp.Models.Entities;

namespace WPFGrowerApp.DataAccess.Services
{
    public interface IGrowerService
    {
        Task<List<GrowerSearchResult>> GetAllGrowersAsync();
        Task<List<GrowerSearchResult>> SearchGrowersAsync(string searchTerm);
        Task<Grower> GetGrowerByNumberAsync(decimal growerNumber);
        Task<bool> SaveGrowerAsync(Grower grower);
    }

    public class GrowerService : IGrowerService
    {
        private readonly Repositories.IGrowerRepository _growerRepository;

        public GrowerService(Repositories.IGrowerRepository growerRepository)
        {
            _growerRepository = growerRepository ?? throw new ArgumentNullException(nameof(growerRepository));
        }

        public async Task<List<GrowerSearchResult>> GetAllGrowersAsync()
        {
            var growers = await _growerRepository.GetAllAsync();
            return growers.Select(MapToGrowerSearchResult).ToList();
        }

        public async Task<List<GrowerSearchResult>> SearchGrowersAsync(string searchTerm)
        {
            var growers = await _growerRepository.SearchAsync(searchTerm);
            return growers.Select(MapToGrowerSearchResult).ToList();
        }

        public async Task<Grower> GetGrowerByNumberAsync(decimal growerNumber)
        {
            var growerEntity = await _growerRepository.GetByNumberAsync(growerNumber);
            return growerEntity != null ? MapToGrowerModel(growerEntity) : null;
        }

        public async Task<bool> SaveGrowerAsync(Grower grower)
        {
            var growerEntity = MapToGrowerEntity(grower);
            return await _growerRepository.SaveAsync(growerEntity);
        }

        private GrowerSearchResult MapToGrowerSearchResult(GrowerEntity entity)
        {
            return new GrowerSearchResult
            {
                GrowerNumber = entity.GrowerNumber,
                GrowerName = entity.GrowerName ?? "",
                ChequeName = entity.ChequeName ?? "",
                City = entity.City ?? "",
                Phone = entity.Phone ?? ""
            };
        }

        private Grower MapToGrowerModel(GrowerEntity entity)
        {
            return new Grower
            {
                GrowerNumber = entity.GrowerNumber,
                GrowerName = entity.GrowerName,
                ChequeName = entity.ChequeName,
                Address = entity.Address,
                City = entity.City,
                Postal = entity.Postal,
                Phone = entity.Phone,
                Acres = entity.Acres ?? 0,
                Notes = entity.Notes,
                Contract = entity.Contract,
                Currency = !string.IsNullOrEmpty(entity.Currency) ? entity.Currency[0] : 'C',
                ContractLimit = entity.ContractLimit.HasValue ? (int)entity.ContractLimit.Value : 0,
                PayGroup = !string.IsNullOrEmpty(entity.PayGroup) && int.TryParse(entity.PayGroup, out int payGroup) ? payGroup : 1,
                OnHold = entity.OnHold ?? false,
                PhoneAdditional1 = entity.PhoneAdditional1,
                OtherNames = entity.OtherNames,
                PhoneAdditional2 = entity.PhoneAdditional2,
                LYFresh = entity.LYFresh ?? 0,
                LYOther = entity.LYOther ?? 0,
                Certified = entity.Certified,
                ChargeGST = entity.ChargeGST ?? false
            };
        }

        private GrowerEntity MapToGrowerEntity(Grower model)
        {
            return new GrowerEntity
            {
                GrowerNumber = model.GrowerNumber,
                Status = 1, // Default status
                GrowerName = model.GrowerName,
                ChequeName = model.ChequeName,
                Address = model.Address,
                City = model.City,
                Province = "", // Not in the original model
                Postal = model.Postal,
                Phone = model.Phone,
                Acres = model.Acres,
                Notes = model.Notes,
                Contract = model.Contract,
                Currency = model.Currency.ToString(),
                ContractLimit = model.ContractLimit,
                PayGroup = model.PayGroup.ToString(),
                OnHold = model.OnHold,
                PhoneAdditional1 = model.PhoneAdditional1,
                AddressLine2 = "", // Not in the original model
                OtherNames = model.OtherNames,
                AltPhone1 = model.PhoneAdditional1,
                AltName2 = "",
                PhoneAdditional2 = model.PhoneAdditional2,
                Note2 = "",
                LYFresh = model.LYFresh,
                LYOther = model.LYOther,
                Certified = model.Certified,
                ChargeGST = model.ChargeGST
            };
        }
    }
}
