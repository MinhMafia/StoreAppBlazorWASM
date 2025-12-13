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

        [HttpPost("register-customer")]
        public async Task<IActionResult> RegisterCustomer([FromBody] CustomerRegisterDTO req)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("❌ ModelState is invalid");
                    return BadRequest(ModelState);
                }

                Console.WriteLine($"📝 Starting registration for: {req.Email}");

                // Kiểm tra email đã tồn tại trong Customers table
                var customerEmailExists = await _db.Customers.AnyAsync(c => c.Email == req.Email);
                if (customerEmailExists)
                {
                    Console.WriteLine($"❌ Email already exists in Customers: {req.Email}");
                    return Conflict(new { message = "Email đã được sử dụng" });
                }

                // Kiểm tra phone đã tồn tại
                if (!string.IsNullOrEmpty(req.Phone))
                {
                    var phoneExists = await _db.Customers.AnyAsync(c => c.Phone == req.Phone);
                    if (phoneExists)
                    {
                        Console.WriteLine($"❌ Phone already exists: {req.Phone}");
                        return Conflict(new { message = "Số điện thoại đã được sử dụng" });
                    }
                }

                // Tạo Customer trực tiếp - KHÔNG TẠO USER
                var customer = new Customer
                {
                    Email = req.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                    FullName = req.FullName,
                    Phone = req.Phone,
                    Address = req.Address,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Customers.Add(customer);
                await _db.SaveChangesAsync();

                Console.WriteLine($"✅ Customer registered - Email: {req.Email}, Customer ID: {customer.Id}");

                return Created("", new
                {
                    message = "Đăng ký thành công",
                    customerId = customer.Id
                });
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"❌ Database update error: {dbEx.Message}");
                Console.WriteLine($"❌ Inner exception: {dbEx.InnerException?.Message}");
                return StatusCode(500, new { message = "Lỗi cơ sở dữ liệu. Vui lòng kiểm tra lại thông tin." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Registration error: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi đăng ký. Vui lòng thử lại." });
            }
        }

        // LOGIN CHO CUSTOMER - TÁCH RIÊNG
        [HttpPost("login-customer")]
        public async Task<IActionResult> LoginCustomer([FromBody] LoginRequest req)
        {
            Console.WriteLine($"🔍 Customer login attempt - Email: '{req.Username}'");

            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest(new { message = "Email và mật khẩu là bắt buộc" });
            }

            // Tìm customer theo email
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == req.Username);

            if (customer == null)
            {
                Console.WriteLine($"❌ Customer not found: {req.Username}");
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });
            }

            // Kiểm tra có password không (khách vãng lai không có)
            if (string.IsNullOrEmpty(customer.PasswordHash))
            {
                Console.WriteLine($"❌ Customer has no password (guest): {req.Username}");
                return Unauthorized(new { message = "Tài khoản chưa được đăng ký" });
            }

            if (!customer.IsActive)
            {
                Console.WriteLine("❌ Customer is inactive");
                return Unauthorized(new { message = "Tài khoản đã bị khóa" });
            }

            bool passwordValid = BCrypt.Net.BCrypt.Verify(req.Password, customer.PasswordHash);
            if (!passwordValid)
            {
                Console.WriteLine("❌ Password incorrect");
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });
            }

            var (token, expiresIn) = _jwt.GenerateCustomerToken(customer);
            Console.WriteLine($"✅ Customer token generated - ID: {customer.Id}");

            return Ok(new AuthResponse
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresIn = expiresIn,
                UserName = customer.FullName,
                Role = "customer"
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
