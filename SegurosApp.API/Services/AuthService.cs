using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SegurosApp.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation("🔐 Intento de login para usuario: {Username}", username);

                // ✅ CONSULTA ADAPTADA A TU ESTRUCTURA
                var user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        (u.Username == username || u.Email == username) &&
                        u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("⚠️ Usuario no encontrado: {Username}", username);
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Usuario o contraseña incorrectos"
                    };
                }

                // ✅ VERIFICAR CONTRASEÑA - Adaptado para tu BD
                bool passwordValid = false;

                if (user.PasswordHash.StartsWith("$2"))
                {
                    // Es un hash BCrypt
                    passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                }
                else
                {
                    // Para testing: comparación directa
                    passwordValid = user.PasswordHash == password;
                }

                if (!passwordValid)
                {
                    _logger.LogWarning("⚠️ Contraseña incorrecta para usuario: {Username}", username);
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Usuario o contraseña incorrectos"
                    };
                }

                // Generar JWT token
                var token = GenerateJwtToken(user);

                // Actualizar último login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Login exitoso para usuario: {Username}", username);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login exitoso",
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(GetTokenExpiryHours()),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        ContactPerson = user.ContactPerson,
                        CompanyName = user.CompanyName
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error durante login para usuario: {Username}", username);
                return new LoginResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                };
            }
        }

        public async Task<ApiResponse<UserDto>> RegisterAsync(RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("📝 Intento de registro para usuario: {Username}", request.Username);

                // Verificar si el usuario ya existe
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

                if (existingUser != null)
                {
                    _logger.LogWarning("⚠️ Usuario ya existe: {Username}", request.Username);
                    return ApiResponse<UserDto>.ErrorResult("El usuario o email ya existe");
                }

                // Crear nuevo usuario adaptado a tu estructura
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    ContactPerson = request.FirstName, 
                    CompanyName = request.LastName,    
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Usuario registrado exitosamente: {Username}", request.Username);

                return ApiResponse<UserDto>.SuccessResult(
                    new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        ContactPerson = user.ContactPerson,
                        CompanyName = user.CompanyName
                    },
                    "Usuario registrado exitosamente"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error durante registro para usuario: {Username}", request.Username);
                return ApiResponse<UserDto>.ErrorResult("Error interno del servidor");
            }
        }

        public async Task<ApiResponse> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return ApiResponse.ErrorResult("Usuario no encontrado");
                }

                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    return ApiResponse.ErrorResult("Contraseña actual incorrecta");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Contraseña cambiada para usuario: {UserId}", userId);
                return ApiResponse.SuccessResult("Contraseña actualizada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cambiando contraseña para usuario: {UserId}", userId);
                return ApiResponse.ErrorResult("Error interno del servidor");
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == userId && u.IsActive)
                    .FirstOrDefaultAsync();

                if (user == null) return null;

                return new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    ContactPerson = user.ContactPerson,
                    CompanyName = user.CompanyName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo usuario: {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
            new Claim(ClaimTypes.Name, user.Username),               
            new Claim(ClaimTypes.Email, user.Email),                
            new Claim(ClaimTypes.Role, "User"),                       
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        }),
                Expires = DateTime.UtcNow.AddHours(GetTokenExpiryHours()),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private int GetTokenExpiryHours()
        {
            return _configuration.GetValue<int>("Jwt:ExpiryInHours", 24);
        }
    }
}