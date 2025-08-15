using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
            _logger.LogInformation("Intento de login para usuario: {Username}", username);

            // Buscar usuario
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("⚠Usuario no encontrado o inactivo: {Username}", username);
                return new LoginResponse
                {
                    Success = false,
                    ErrorMessage = "Usuario o contraseña incorrectos"
                };
            }

            // Verificar contraseña
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("⚠Contraseña incorrecta para usuario: {Username}", username);
                return new LoginResponse
                {
                    Success = false,
                    ErrorMessage = "Usuario o contraseña incorrectos"
                };
            }

            // Actualizar último login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generar JWT
            var token = GenerateJwtToken(user);

            _logger.LogInformation("Login exitoso para usuario: {Username}", username);

            return new LoginResponse
            {
                Success = true,
                Token = token,
                User = MapToUserDto(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante login para usuario: {Username}", username);
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = "Error interno del servidor"
            };
        }
    }

    public async Task<ApiResponse<UserDto>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("Registrando nuevo usuario: {Username}", request.Username);

            // Verificar si ya existe el usuario
            var existingUser = await _context.Users
                .AnyAsync(u => u.Username == request.Username || u.Email == request.Email);

            if (existingUser)
            {
                return ApiResponse<UserDto>.ErrorResult("El usuario o email ya existe");
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email ?? string.Empty, 
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CompanyName = request.CompanyName ?? "Sin especificar",
                CompanyAddress = request.CompanyAddress,
                CompanyRUC = request.CompanyRUC,
                ContactPerson = request.ContactPerson,
                ContactPhone = request.ContactPhone,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuario registrado exitosamente: {Username} (ID: {Id})",
                user.Username, user.Id);

            return ApiResponse<UserDto>.SuccessResult(
                MapToUserDto(user),
                "Usuario registrado exitosamente"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registrando usuario: {Username}", request.Username);
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
                return ApiResponse.Error("Usuario no encontrado");
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                return ApiResponse.Error("Contraseña actual incorrecta");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Contraseña cambiada para usuario ID: {UserId}", userId);
            return ApiResponse.SuccessResponse("Contraseña actualizada exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cambiando contraseña para usuario ID: {UserId}", userId);
            return ApiResponse.Error("Error interno del servidor");
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            return user != null ? MapToUserDto(user) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo usuario ID: {UserId}", userId);
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

    // ===============================
    // MÉTODOS PRIVADOS
    // ===============================

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
                new Claim("userId", user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("email", user.Email),
                new Claim("companyName", user.CompanyName),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

        var expiryHours = _configuration.GetValue<int>("Jwt:ExpiryInHours", 24);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CompanyName = user.CompanyName,
            ContactPerson = user.ContactPerson,
            ContactPhone = user.ContactPhone,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        };
    }
}
