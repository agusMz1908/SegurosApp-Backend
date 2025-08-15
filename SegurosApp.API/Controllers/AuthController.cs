using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SegurosApp.API.DTOs;
using SegurosApp.API.Services;
using System.Security.Claims;

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
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("🔐 Intento de login desde IP: {IP} para usuario: {Username}",
                    HttpContext.Connection.RemoteIpAddress, request.Username);

                var result = await _authService.LoginAsync(request.Username, request.Password);

                if (!result.Success)
                {
                    _logger.LogWarning("⚠️ Login fallido para usuario: {Username}", request.Username);
                    return Unauthorized(result);
                }

                _logger.LogInformation("✅ Login exitoso para usuario: {Username}", request.Username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en login para usuario: {Username}", request.Username);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<UserDto>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("📝 Intento de registro desde IP: {IP} para usuario: {Username}",
                    HttpContext.Connection.RemoteIpAddress, request.Username);

                var result = await _authService.RegisterAsync(request);

                if (!result.Success)
                {
                    _logger.LogWarning("⚠️ Registro fallido para usuario: {Username} - {Error}",
                        request.Username, result.ErrorMessage);
                    return BadRequest(result);
                }

                _logger.LogInformation("✅ Usuario registrado exitosamente: {Username}", request.Username);
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
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo usuario actual");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(200)]
        public ActionResult Logout()
        {
            try
            {
                var username = User.FindFirst("username")?.Value ?? "Unknown";
                _logger.LogInformation("🚪 Logout para usuario: {Username}", username);

                return Ok(new
                {
                    success = true,
                    message = "Sesión cerrada exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en logout");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}