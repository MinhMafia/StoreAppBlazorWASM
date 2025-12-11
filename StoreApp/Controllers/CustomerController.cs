using Microsoft.AspNetCore.Mvc;
using StoreApp.Services;
using StoreApp.Shared;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly CustomerService _service;
        private readonly UserService _userService;

        public CustomersController(CustomerService service,UserService userService)
        {
            _service = service;
            _userService = userService;
        }

        // GET api/customers/paginated?page=1&pageSize=10=>CODE CŨ
        // [HttpGet("paginated")]
        // public async Task<ActionResult> GetPaginated(
        //     [FromQuery] int page = 1,
        //     [FromQuery] int pageSize = 10)
        // {
        //     var (items, totalPages) = await _service.GetPaginatedAsync(page, pageSize);
        //     return Ok(new { items, totalPages });
        // }
        [HttpGet("paginated")]
        public async Task<ActionResult> GetPaginated(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var result = await _service.GetPaginatedAsync(page, pageSize, search);
            return Ok(result);
        }

        // GET api/customers/search?keyword=...
        [HttpGet("search")]
        public async Task<ActionResult> Search([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest("Keyword is required");

            var items = await _service.SearchByNameAsync(keyword);
            return Ok(items);
        }

        //GET api/customers?page=1&pageSize=10&keyword=
        [HttpGet("")]
        public async Task<ActionResult> GetFilteredAndPaginatedCustomers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null,
            [FromQuery] string? status = null)
        {
            var result = await _service.GetFilteredAndPaginatedAsync(page, pageSize, keyword, status);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetCustomerById(int id)
        {
            var result = await _service.GetCustomerByIdAsync(id);
            if (!result.Success && result.StatusCode == 404)
            {
                return NotFound(result.Errors);
            }
            return Ok(result.Data);
        }

        // GET api/customers/by-user/{userId}
        [HttpGet("by-user/{userId:int}")]
        public async Task<ActionResult> GetCustomerByUserId(int userId)
        {
            var result = await _service.GetCustomerByUserIdAsync(userId);
            if (!result.Success && result.StatusCode == 404)
            {
                return NotFound(result.Errors);
            }
            return Ok(result.Data);
        }

        // POST api/customers
        [HttpPost("")]
        public async Task<ActionResult> CreateCustomer([FromBody] CustomerCreateDTO createDto)
        {
            if (createDto == null) return BadRequest("Payload is required.");

            // Nếu chưa có UserId thì tạo mới user với role "customer", sau đó gán UserId cho customer
            if (!createDto.UserId.HasValue)
            {
                // Chuẩn hóa dữ liệu user đầu vào
                var username = !string.IsNullOrWhiteSpace(createDto.Phone)
                    ? createDto.Phone.Trim()
                    : (!string.IsNullOrWhiteSpace(createDto.Email) ? createDto.Email.Trim() : $"customer_{Guid.NewGuid():N}");

                var email = !string.IsNullOrWhiteSpace(createDto.Email)
                    ? createDto.Email.Trim()
                    : $"{username}@auto.local";

                var password = !string.IsNullOrWhiteSpace(createDto.Phone)
                    ? createDto.Phone.Trim()
                    : "123456";

                var userDto = new UserDTO
                {
                    Username = username,
                    Email = email,
                    FullName = createDto.FullName,
                    Role = "customer",
                    IsActive = true,
                    IsLocked = false,
                    Password = password
                };

                // Kiểm tra trùng username/email
                if (await _userService.UserExistsUsernameAsync(userDto.Username))
                    return Conflict("Username already exists.");
                if (await _userService.UserExistsEmailAsync(userDto.Email))
                    return Conflict("Email already exists.");

                var createdUser = await _userService.CreateUserAsync(userDto);
                if (createdUser == null)
                    return StatusCode(500, "Unable to create user.");

                createDto.UserId = createdUser.Id;
            }

            var result = await _service.CreateCustomerAsync(createDto);
            if (!result.Success && result.StatusCode == 409)
            {
                return Conflict(result.Errors);
            }
            return StatusCode(201, result.Data);
        }

        // PATCH api/customers/{id}
        [HttpPatch("{id:int}")]
        public async Task<ActionResult> UpdateCustomer(int id, [FromBody] CustomerUpdateDTO updateDto)
        {
            var result = await _service.UpdateCustomerAsync(id, updateDto);
            if (!result.Success && result.StatusCode == 404)
            {
                return NotFound(result.Errors);
            }
            return Ok(result.Data);
        }

        // PUT api/customers/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateActiveCustomer(int id, [FromBody] CustomerUpdateActiveDTO activeDto)
        {
            Console.WriteLine($"[CONTROLLER] UpdateActiveCustomerAsync called with id={id}, isActive={activeDto.IsActive}");
            var result = await _service.UpdateActiveCustomerAsync(id, activeDto.IsActive);
            if (!result.Success && result.StatusCode == 404)
            {
                return NotFound(result.Errors);
            }
            return Ok(result.Data);
        }
    }
}
