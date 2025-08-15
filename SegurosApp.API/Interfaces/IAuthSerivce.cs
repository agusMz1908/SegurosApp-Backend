using SegurosApp.API.DTOs;
using SegurosApp.API.Models;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string username, string password);
    Task<ApiResponse<UserDto>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<bool> ValidateTokenAsync(string token);
}