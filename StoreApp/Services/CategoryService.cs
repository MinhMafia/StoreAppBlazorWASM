using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StoreApp.Shared;
using StoreApp.Models;
using StoreApp.Repository;

namespace StoreApp.Services
{
    public class CategoryService
    {
        private readonly CategoryRepository _repo;

        public CategoryService(CategoryRepository repo)
        {
            _repo = repo;
        }

        private CategoryDTO MapToDto(Category c)
        {
            return new CategoryDTO
            {
                Id = c.Id,
                Name = c.Name,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsActive = c.IsActive
            };
        }

        public async Task<List<CategoryDTO>> GetAllAsync()
        {
            var items = await _repo.GetAllAsync();
            return items.Select(MapToDto).ToList();
        }

        public async Task<CategoryDTO?> GetByIdAsync(int id)
        {
            var c = await _repo.GetByIdAsync(id);
            return c == null ? null : MapToDto(c);
        }

        // Paginated result: reuse your existing PaginationResult<T>
        public async Task<PaginationResult<CategoryDTO>> GetPaginatedAsync(int page, int pageSize, string? search)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var (items, total) = await _repo.GetPaginatedAsync(page, pageSize, search);
            var dtoItems = items.Select(MapToDto).ToList();

            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            return new PaginationResult<CategoryDTO>
            {
                Items = dtoItems,
                TotalItems = total,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPrevious = page > 1,
                HasNext = page < totalPages
            };
        }


        // Lấy category với filter và phân trang (ServiceResultDTO)
        public async Task<ResultPaginatedDTO<CategoryResponseDTO>> GetFilteredAndPaginatedAsync(
            int page,
            int pageSize,
            string? keyword = null,
            string? status = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var skip = (page - 1) * pageSize;

            var categories = await _repo.FindCategoriesByFilteredAndPaginatedAsync(skip, pageSize, keyword, status);
            var totalItems = await _repo.CountFilteredAndPaginatedAsync(keyword, status);
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var dtos = categories.Select(MapToCategoryResponseDto).ToList();

            return new ResultPaginatedDTO<CategoryResponseDTO>
            {
                Items = dtos,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems
            };
        }

        // Lấy category by Id với ServiceResultDTO
        public async Task<ServiceResultDTO<CategoryResponseDTO>> GetCategoryByIdAsync(int id)
        {
            var category = await _repo.GetByIdAsync(id);
            if (category == null)
            {
                return ServiceResultDTO<CategoryResponseDTO>.CreateFailureResult(404, "Category not found.");
            }

            return ServiceResultDTO<CategoryResponseDTO>.CreateSuccessResult(MapToCategoryResponseDto(category), 200);
        }

        // Tạo category mới với ServiceResultDTO
        public async Task<ServiceResultDTO<CategoryResponseDTO>> CreateCategoryWithDtoAsync(CategoryCreateDTO createDto)
        {
            // Kiểm tra trùng tên
            var exists = await _repo.ExistsByNameAsync(createDto.Name);
            if (exists)
            {
                return ServiceResultDTO<CategoryResponseDTO>.CreateFailureResult(409, "Category name already exists.");
            }

            var category = new Category
            {
                Name = createDto.Name,
                Description = createDto.Description,
                Slug = GenerateSlug(createDto.Name),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var newCategory = await _repo.CreateAsync(category);
            return ServiceResultDTO<CategoryResponseDTO>.CreateSuccessResult(MapToCategoryResponseDto(newCategory), 201);
        }

        // Cập nhật category với ServiceResultDTO
        public async Task<ServiceResultDTO<CategoryResponseDTO>> UpdateCategoryWithDtoAsync(int id, CategoryUpdateDTO updateDto)
        {
            var category = await _repo.GetByIdAsync(id);
            if (category == null)
            {
                return ServiceResultDTO<CategoryResponseDTO>.CreateFailureResult(404, "Category not found.");
            }

            var hasChanges = false;

            if (updateDto.Name != null && updateDto.Name != category.Name)
            {
                // Kiểm tra trùng tên
                var duplicate = await _repo.ExistsByNameAsync(updateDto.Name, id);
                if (duplicate)
                {
                    return ServiceResultDTO<CategoryResponseDTO>.CreateFailureResult(409, "Category name already exists.");
                }

                category.Name = updateDto.Name;
                category.Slug = GenerateSlug(updateDto.Name);
                hasChanges = true;
            }

            if (updateDto.Description != null && updateDto.Description != category.Description)
            {
                category.Description = updateDto.Description;
                hasChanges = true;
            }

            if (!hasChanges)
            {
                return ServiceResultDTO<CategoryResponseDTO>.CreateFailureResult(400, "No changes detected.");
            }

            category.UpdatedAt = DateTime.UtcNow;

            var updatedCategory = await _repo.UpdateAsync(category);
            return ServiceResultDTO<CategoryResponseDTO>.CreateSuccessResult(
                MapToCategoryResponseDto(updatedCategory),
                200
            );
        }

        // Toggle active status với ServiceResultDTO
        public async Task<ServiceResultDTO<CategoryResponseDTO>> UpdateActiveCategoryAsync(int id, bool isActive)
        {
            var category = await _repo.GetByIdAsync(id);
            if (category == null)
            {
                return ServiceResultDTO<CategoryResponseDTO>.CreateFailureResult(404, "Category not found.");
            }

            if (category.IsActive == isActive)
            {
                return ServiceResultDTO<CategoryResponseDTO>.CreateFailureResult(400, "No changes detected.");
            }

            category.IsActive = isActive;
            category.UpdatedAt = DateTime.UtcNow;

            var updatedCategory = await _repo.UpdateAsync(category);
            return ServiceResultDTO<CategoryResponseDTO>.CreateSuccessResult(MapToCategoryResponseDto(updatedCategory), 200);
        }

        // Map sang CategoryResponseDTO
        public CategoryResponseDTO MapToCategoryResponseDto(Category category)
        {
            return new CategoryResponseDTO
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };
        }

        // Tạo slug từ name
        private string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Chuyển thành lowercase và thay khoảng trắng bằng dấu gạch ngang
            var slug = name.Trim().ToLower()
                .Replace(" ", "-")
                .Replace(".", "")
                .Replace(",", "");

            return slug;
        }
    }
}
