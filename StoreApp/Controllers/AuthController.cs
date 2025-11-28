using StoreApp.Data;
using StoreApp.Shared;
using StoreApp.Models;
using StoreApp.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly JwtService _jwt;

        public AuthController(AppDbContext db, JwtService jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Username and password required.");

            var exists = await _db.Users.AnyAsync(u => u.Username == req.Username);
            if (exists) return BadRequest("Username already exists.");

            var user = new User
            {
                Username = req.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                FullName = req.Username,
                Role = "staff",
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Created("", new { message = "Created" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            Console.WriteLine($"🔍 Login attempt - Username: '{req.Username}', Password length: {req.Password?.Length ?? 0}");

            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                Console.WriteLine("❌ Username or password is empty");
                return BadRequest(new { message = "Username and password are required" });
            }

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == req.Username);

            if (user == null)
            {
                Console.WriteLine($"❌ User not found: {req.Username}");
                return Unauthorized(new { message = "Invalid username or password" });
            }

            Console.WriteLine($"✅ User found - ID: {user.Id}, IsActive: {user.IsActive}");

            if (!user.IsActive)
            {
                Console.WriteLine("❌ User is inactive");
                return Unauthorized(new { message = "Account is disabled" });
            }

            bool passwordValid = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
            Console.WriteLine($"🔐 Password verification: {passwordValid}");

            if (!passwordValid)
            {
                Console.WriteLine("❌ Password incorrect");
                return Unauthorized(new { message = "Invalid username or password" });
            }

            var (token, expiresIn) = _jwt.GenerateToken(user);
            Console.WriteLine($"✅ Token generated successfully");

            return Ok(new AuthResponse
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresIn = expiresIn,
                UserName = user.Username,
                Role = user.Role
            });
        }
        [HttpPost("reset-admin-password")]
        public async Task<IActionResult> ResetAdminPassword()
        {
            var admin = await _db.Users.FirstOrDefaultAsync(u => u.Username == "admin");

            if (admin == null)
            {
                return NotFound(new { message = "Admin user not found" });
            }

            // Đổi password thành "admin123"
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            await _db.SaveChangesAsync();

            Console.WriteLine($"✅ Admin user found - ID: {admin.Id}");

            Console.WriteLine("✅ Admin password reset to: admin123");
            return Ok(new { message = "Admin password reset to 'admin123'" });
        }
    }
}
