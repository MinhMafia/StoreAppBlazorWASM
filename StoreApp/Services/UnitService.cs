using StoreApp.Data;
using StoreApp.Models;
using StoreApp.Shared;
using StoreApp.Repository;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Services
{
    public class UnitService
    {
        private readonly UnitRepository _unitRepo;

        public UnitService(UnitRepository unitRepo)
        {
            _unitRepo = unitRepo;
        }

        public async Task<List<UnitDTO>> GetAllUnitsAsync(bool? isActive = null)
        {
            var units = await _unitRepo.GetAllAsync(isActive);
            var productCounts = await _unitRepo.GetProductCountByUnitAsync();

            return units.Select(u => new UnitDTO
            {
                Id = u.Id,
                Code = u.Code,
                Name = u.Name,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                ProductCount = productCounts.GetValueOrDefault(u.Id, 0)
            }).ToList();
        }

        public async Task<UnitDTO?> GetUnitByIdAsync(int id)
        {
            var unit = await _unitRepo.GetByIdAsync(id);
            if (unit == null) return null;

            return new UnitDTO
            {
                Id = unit.Id,
                Code = unit.Code,
                Name = unit.Name,
                IsActive = unit.IsActive,
                CreatedAt = unit.CreatedAt,
                UpdatedAt = unit.UpdatedAt,
                ProductCount = unit.Products.Count
            };
        }

        public async Task<UnitDTO> CreateUnitAsync(UnitDTO dto)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new ArgumentException("Code is required");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required");

            // Check duplicate
            var existing = await _unitRepo.GetByCodeAsync(dto.Code);
            if (existing != null)
                throw new InvalidOperationException("Unit code already exists");

            var unit = new Unit
            {
                Code = dto.Code,
                Name = dto.Name,
                IsActive = dto.IsActive
            };

            var created = await _unitRepo.CreateAsync(unit);

            return new UnitDTO
            {
                Id = created.Id,
                Code = created.Code,
                Name = created.Name,
                IsActive = created.IsActive,
                CreatedAt = created.CreatedAt,
                UpdatedAt = created.UpdatedAt,
                ProductCount = 0
            };
        }

        public async Task<UnitDTO?> UpdateUnitAsync(int id, UnitDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new ArgumentException("Code is required");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required");

            // Check duplicate (exclude current)
            var existingByCode = await _unitRepo.GetByCodeAsync(dto.Code);
            if (existingByCode != null && existingByCode.Id != id)
                throw new InvalidOperationException("Unit code already exists");

            var unit = new Unit
            {
                Code = dto.Code,
                Name = dto.Name,
                IsActive = dto.IsActive
            };

            var updated = await _unitRepo.UpdateAsync(id, unit);
            if (updated == null) return null;

            return new UnitDTO
            {
                Id = updated.Id,
                Code = updated.Code,
                Name = updated.Name,
                IsActive = updated.IsActive,
                CreatedAt = updated.CreatedAt,
                UpdatedAt = updated.UpdatedAt
            };
        }

        public async Task<bool> DeleteUnitAsync(int id)
        {
            return await _unitRepo.DeleteAsync(id);
        }

        public async Task<UnitDTO?> ToggleActiveAsync(int id)
        {
            var unit = await _unitRepo.GetByIdAsync(id);
            if (unit == null) return null;

            unit.IsActive = !unit.IsActive;
            unit.UpdatedAt = DateTime.UtcNow;

            var updated = await _unitRepo.UpdateAsync(id, unit);

            return new UnitDTO
            {
                Id = updated!.Id,
                Code = updated.Code,
                Name = updated.Name,
                IsActive = updated.IsActive,
                CreatedAt = updated.CreatedAt,
                UpdatedAt = updated.UpdatedAt
            };
        }
    }
}