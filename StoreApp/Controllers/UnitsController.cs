using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StoreApp.Repository;
using StoreApp.Models;
using StoreApp.Shared;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize]
    public class UnitsController : ControllerBase
    {
        private readonly UnitRepository _unitRepo;

        public UnitsController(UnitRepository unitRepo)
        {
            _unitRepo = unitRepo;
        }

        // GET api/units
        [HttpGet]
        public async Task<ActionResult<List<UnitDTO>>> GetAll([FromQuery] bool? isActive = null)
        {
            var units = await _unitRepo.GetAllAsync(isActive);
            var productCounts = await _unitRepo.GetProductCountByUnitAsync();

            var result = units.Select(u => new UnitDTO
            {
                Id = u.Id,
                Code = u.Code,
                Name = u.Name,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                ProductCount = productCounts.GetValueOrDefault(u.Id, 0)
            }).ToList();

            return Ok(result);
        }

        // GET api/units/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UnitDTO>> GetById(int id)
        {
            var unit = await _unitRepo.GetByIdAsync(id);
            if (unit == null)
                return NotFound(new { message = "Unit not found" });

            var dto = new UnitDTO
            {
                Id = unit.Id,
                Code = unit.Code,
                Name = unit.Name,
                IsActive = unit.IsActive,
                CreatedAt = unit.CreatedAt,
                UpdatedAt = unit.UpdatedAt,
                ProductCount = unit.Products.Count
            };

            return Ok(dto);
        }

        // POST api/units
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<UnitDTO>> Create([FromBody] UnitDTO dto)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest(new { message = "Code is required" });

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Name is required" });

            // Kiểm tra trùng code
            var existing = await _unitRepo.GetByCodeAsync(dto.Code);
            if (existing != null)
                return BadRequest(new { message = "Unit code already exists" });

            var unit = new Unit
            {
                Code = dto.Code,
                Name = dto.Name,
                IsActive = dto.IsActive
            };

            var created = await _unitRepo.CreateAsync(unit);

            var resultDto = new UnitDTO
            {
                Id = created.Id,
                Code = created.Code,
                Name = created.Name,
                IsActive = created.IsActive,
                CreatedAt = created.CreatedAt,
                UpdatedAt = created.UpdatedAt,
                ProductCount = 0
            };

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, resultDto);
        }

        // PUT api/units/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<UnitDTO>> Update(int id, [FromBody] UnitDTO dto)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest(new { message = "Code is required" });

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Name is required" });

            // Kiểm tra trùng code (trừ chính nó)
            var existingByCode = await _unitRepo.GetByCodeAsync(dto.Code);
            if (existingByCode != null && existingByCode.Id != id)
                return BadRequest(new { message = "Unit code already exists" });

            var unit = new Unit
            {
                Code = dto.Code,
                Name = dto.Name,
                IsActive = dto.IsActive
            };

            var updated = await _unitRepo.UpdateAsync(id, unit);
            if (updated == null)
                return NotFound(new { message = "Unit not found" });

            var resultDto = new UnitDTO
            {
                Id = updated.Id,
                Code = updated.Code,
                Name = updated.Name,
                IsActive = updated.IsActive,
                CreatedAt = updated.CreatedAt,
                UpdatedAt = updated.UpdatedAt
            };

            return Ok(resultDto);
        }

        // DELETE api/units/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _unitRepo.DeleteAsync(id);
                if (!deleted)
                    return NotFound(new { message = "Unit not found" });

                return Ok(new { message = "Unit deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH api/units/{id}/toggle-active
        [HttpPatch("{id}/toggle-active")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<UnitDTO>> ToggleActive(int id)
        {
            var unit = await _unitRepo.GetByIdAsync(id);
            if (unit == null)
                return NotFound(new { message = "Unit not found" });

            unit.IsActive = !unit.IsActive;
            unit.UpdatedAt = DateTime.UtcNow;

            var updated = await _unitRepo.UpdateAsync(id, unit);

            var dto = new UnitDTO
            {
                Id = updated!.Id,
                Code = updated.Code,
                Name = updated.Name,
                IsActive = updated.IsActive,
                CreatedAt = updated.CreatedAt,
                UpdatedAt = updated.UpdatedAt
            };

            return Ok(dto);
        }
    }
}