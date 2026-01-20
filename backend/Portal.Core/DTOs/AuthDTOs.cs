namespace Portal.Core.DTOs;

public record LoginRequest(string Username, string Password);

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName
);

public record AuthResponse(
    int UserId,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiration,
    List<string> Permissions,
    List<string> Roles
);

public record RefreshTokenRequest(string RefreshToken);

public record UserInfoResponse(
    int UserId,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    List<string> Permissions,
    List<string> Roles,
    List<ModuleInfo> Modules
);

public record ModuleInfo(string Code, string Name, string? Icon, List<string> Permissions);

// Email dogrulama ile kayit icin yeni DTO'lar
public record RegisterResponse(
    bool Success,
    string Message,
    bool RequiresEmailVerification,
    string? Username,
    string? Email,
    string? FirstName,
    string? LastName
);

public record EmailLookupResponse(
    bool Found,
    string? FirstName,
    string? LastName,
    string? Department,
    string? Position,
    string? Title,
    string? Phone
);

public record VerifyEmailRequest(string Token);

public record VerifyEmailResponse(
    bool Success,
    string Message,
    AuthResponse? AuthData
);

public record ResendVerificationRequest(string Email);

// Şifre değiştirme
public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record ChangePasswordResponse(
    bool Success,
    string Message
);

// Şifremi unuttum
public record ForgotPasswordRequest(string Email);

public record ForgotPasswordResponse(
    bool Success,
    string Message
);

public record ResetPasswordRequest(
    string Token,
    string NewPassword
);

public record ResetPasswordResponse(
    bool Success,
    string Message
);
