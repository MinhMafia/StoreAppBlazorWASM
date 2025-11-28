using Microsoft.AspNetCore.Mvc;
using StoreApp.Services;
using StoreApp.Shared;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        // GET api/users/paginated?page=1&pageSize=10
        [HttpGet("paginated")]
        public async Task<ActionResult> GetPaginated(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            pageSize = Math.Clamp(pageSize, 1, 100);

            var users = await _userService.GetPaginatedAsync(page, pageSize);
            return Ok(users);
        }

        [HttpGet("getalluser")]
        public async Task<ActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult> CreateUser([FromBody] UserDTO request)
        {
            NormalizeUserDto(request);
            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                ModelState.AddModelError(nameof(UserDTO.Password), "Password is required.");
            }

            if (!TryValidateModel(request))
                return ValidationProblem(ModelState);

            if (await _userService.UserExistsUsernameAsync(request.Username))
                return Conflict("Username already exists.");

            if (await _userService.UserExistsEmailAsync(request.Email))
                return Conflict("Email already exists.");

            var createdUser = await _userService.CreateUserAsync(request);
            if (createdUser == null)
                return StatusCode(500, "Unable to create user.");

            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] UserDTO request)
        {
            if (id <= 0)
                return BadRequest("User ID must be greater than 0.");

            NormalizeUserDto(request);
            ModelState.Clear();

            if (!TryValidateModel(request))
                return ValidationProblem(ModelState);

            if (await _userService.UserExistsUsernameAsync(request.Username, id))
                return Conflict("Username already exists.");

            if (await _userService.UserExistsEmailAsync(request.Email, id))
                return Conflict("Email already exists.");

            var updatedUser = await _userService.UpdateUserAsync(id, request);
            if (updatedUser == null)
                return NotFound($"User with ID {id} not found");

            return Ok(updatedUser);
        }

        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult> UpdateStatus(int id, [FromQuery] bool isActive)
        {
            if (id <= 0)
                return BadRequest("User ID must be greater than 0.");

            var updatedUser = await _userService.UpdateStatusAsync(id, isActive);
            if (updatedUser == null)
                return NotFound($"User with ID {id} not found");

            return Ok(updatedUser);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            if (id <= 0)
                return BadRequest("User ID must be greater than 0.");

            var deleted = await _userService.DeleteUserAsync(id);
            if (!deleted)
                return NotFound($"User with ID {id} not found");

            return NoContent();
        }

        private static void NormalizeUserDto(UserDTO request)
        {
            request.Username = request.Username?.Trim() ?? string.Empty;
            request.Email = request.Email?.Trim() ?? string.Empty;
            request.FullName = string.IsNullOrWhiteSpace(request.FullName)
                ? null
                : request.FullName.Trim();
        }
    }
}
