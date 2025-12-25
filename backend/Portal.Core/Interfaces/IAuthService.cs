using Portal.Core.DTOs;

namespace Portal.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, string? ipAddress);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken, string? ipAddress);
    Task RevokeTokenAsync(string refreshToken, string? ipAddress);
    Task<UserInfoResponse?> GetUserInfoAsync(int userId);
}
