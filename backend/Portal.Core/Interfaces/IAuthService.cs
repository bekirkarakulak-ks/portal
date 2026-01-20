using Portal.Core.DTOs;

namespace Portal.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, string? ipAddress);
    Task<RegisterResponse> RegisterWithVerificationAsync(RegisterRequest request, string? ipAddress);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken, string? ipAddress);
    Task RevokeTokenAsync(string refreshToken, string? ipAddress);
    Task<UserInfoResponse?> GetUserInfoAsync(int userId);
    Task<UserInfoResponse?> GetUserInfoByUsernameAsync(string username);
    Task<EmailLookupResponse> CheckEmailEligibilityAsync(string email);
    Task<VerifyEmailResponse> VerifyEmailAsync(string token, string? ipAddress);
    Task<bool> ResendVerificationEmailAsync(string email);

    // Şifre işlemleri
    Task<ChangePasswordResponse> ChangePasswordAsync(string username, ChangePasswordRequest request);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);
}
