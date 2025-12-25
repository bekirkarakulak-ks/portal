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
