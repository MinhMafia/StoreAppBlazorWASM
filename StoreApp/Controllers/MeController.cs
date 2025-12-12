using System.ComponentModel.DataAnnotations;
using StoreApp.Shared;
using StoreApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/me")]
    [Authorize]
    public class MeController : ControllerBase
    {
        private readonly UserService _userService;

        public MeController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult> GetProfile()
        {
            var userId = ResolveUserId();
            if (userId == null)
                return Unauthorized("Missing X-User-Id header.");

            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user == null)
                return NotFound("User not found.");

            return Ok(MapToMeDto(user));
        }

        [HttpPut]
        public async Task<ActionResult> UpdateProfile([FromBody] MeDTO request)
        {
            var userId = ResolveUserId();
            if (userId == null)
                return Unauthorized("Missing X-User-Id header.");

            ModelState.Remove(nameof(MeDTO.Username));
            ModelState.Remove(nameof(MeDTO.Email));
            ModelState.Remove(nameof(MeDTO.FullName));

            ValidateProfilePayload(request);
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (await _userService.UserExistsUsernameAsync(request.Username, userId.Value))
                return Conflict("Username already exists.");

            if (await _userService.UserExistsEmailAsync(request.Email, userId.Value))
                return Conflict("Email already exists.");

            var updated = await _userService.UpdateProfileAsync(userId.Value, request);
            if (updated == null)
                return NotFound("User not found.");

            return Ok(MapToMeDto(updated));
        }

        [HttpPut("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] MeDTO request)
        {
            var userId = ResolveUserId();
            if (userId == null)
                return Unauthorized("Missing X-User-Id header.");

            // Ignore profile fields when validating password-only payloads
            ModelState.Remove(nameof(MeDTO.Username));
            ModelState.Remove(nameof(MeDTO.Email));
            ModelState.Remove(nameof(MeDTO.FullName));

            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                ModelState.AddModelError(nameof(MeDTO.CurrentPassword), "Current password is required.");

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                ModelState.AddModelError(nameof(MeDTO.NewPassword), "New password is required.");

            if (string.IsNullOrWhiteSpace(request.ConfirmNewPassword))
                ModelState.AddModelError(nameof(MeDTO.ConfirmNewPassword), "Password confirmation is required.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _userService.ChangePasswordAsync(
                userId.Value,
                request.CurrentPassword!,
                request.NewPassword!
            );

            if (result == UserService.ChangePasswordResult.UserNotFound)
                return NotFound("User not found.");

            if (result == UserService.ChangePasswordResult.InvalidCurrentPassword)
                return BadRequest("Current password is incorrect.");

            return NoContent();
        }

        private int? ResolveUserId()
        {
            // ƒ??u tiA?n lA?y id trong claim
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(idClaim) && int.TryParse(idClaim, out var claimUserId))
            {
                return claimUserId;
            }

            // fallback header (nA?u front A?t header cA?u hA??i cA? lA?y)
            if (Request.Headers.TryGetValue("X-User-Id", out var header) &&
                int.TryParse(header, out var userId))
            {
                return userId;
            }

            return null;
        }

        private static MeDTO MapToMeDto(UserDTO user)
        {
            return new MeDTO
            {
                Username = user.Username ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName
            };
        }

        private void ValidateProfilePayload(MeDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                ModelState.AddModelError(nameof(MeDTO.Username), "Username is required.");
            }
            else if (request.Username.Length < 3)
            {
                ModelState.AddModelError(nameof(MeDTO.Username), "Username must be at least 3 characters.");
            }
            else if (request.Username.Length > 150)
            {
                ModelState.AddModelError(nameof(MeDTO.Username), "Username must be at most 150 characters.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                ModelState.AddModelError(nameof(MeDTO.Email), "Email is required.");
            }
            else if (!new EmailAddressAttribute().IsValid(request.Email))
            {
                ModelState.AddModelError(nameof(MeDTO.Email), "Email is invalid.");
            }
            else if (request.Email.Length > 255)
            {
                ModelState.AddModelError(nameof(MeDTO.Email), "Email must be at most 255 characters.");
            }
        }
    }
}
