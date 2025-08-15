using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SegurosApp.API.DTOs;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Models;

namespace SegurosApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(typeof(LoginResponse), 400)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Datos de entrada inválidos"
                    });
                }

                var result = await _authService.LoginAsync(request.Username, request.Password);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en login para usuario: {Username}", request.Username);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 400)]
        public async Task<ActionResult<ApiResponse<UserDto>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResult("Datos de entrada inválidos"));
                }

                var result = await _authService.RegisterAsync(request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en registro para usuario: {Username}", request.Username);
                return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Error interno del servidor"));
            }
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(ApiResponse<UserDto>.ErrorResult("Token inválido"));
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(ApiResponse<UserDto>.ErrorResult("Usuario no encontrado"));
                }

                return Ok(ApiResponse<UserDto>.SuccessResult(user, "Usuario obtenido exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo usuario actual");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Error interno del servidor"));
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse.ErrorResult("Datos de entrada inválidos"));
                }

                var userIdClaim = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(ApiResponse.ErrorResult("Token inválido"));
                }

                var result = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cambiando contraseña");
                return StatusCode(500, ApiResponse.ErrorResult("Error interno del servidor"));
            }
        }

        [HttpPost("validate-token")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<ActionResult<ApiResponse>> ValidateToken([FromBody] ValidateTokenRequest request)
        {
            try
            {
                var isValid = await _authService.ValidateTokenAsync(request.Token);

                return Ok(new ApiResponse
                {
                    Success = isValid,
                    Message = isValid ? "Token válido" : "Token inválido"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error validando token");
                return StatusCode(500, ApiResponse.ErrorResult("Error interno del servidor"));
            }
        }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ValidateTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}