using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Portal.Core.DTOs;
using Portal.Core.Entities;
using Portal.Core.Interfaces;
using Portal.Infrastructure.Data;

namespace Portal.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly PortalDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPermissionService _permissionService;

    public AuthService(
        PortalDbContext context,
        IConfiguration configuration,
        IPermissionService permissionService)
    {
        _context = context;
        _configuration = configuration;
        _permissionService = permissionService;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTime.UtcNow;

        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id);
        var roles = await _permissionService.GetUserRolesAsync(user.Id);

        var (accessToken, expiration) = GenerateAccessToken(user, permissions);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress);

        await _context.SaveChangesAsync();

        return new AuthResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            accessToken,
            refreshToken.Token,
            expiration,
            permissions,
            roles
        );
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, string? ipAddress)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
            return null;

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assign default role (CALISAN)
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "CALISAN");
        if (defaultRole != null)
        {
            _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = defaultRole.Id });
            await _context.SaveChangesAsync();
        }

        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id);
        var roles = await _permissionService.GetUserRolesAsync(user.Id);

        var (accessToken, expiration) = GenerateAccessToken(user, permissions);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress);

        return new AuthResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            accessToken,
            refreshToken.Token,
            expiration,
            permissions,
            roles
        );
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken, string? ipAddress)
    {
        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null || !token.IsActive)
            return null;

        // Revoke old token
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;

        // Generate new tokens
        var user = token.User;
        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id);
        var roles = await _permissionService.GetUserRolesAsync(user.Id);

        var (accessToken, expiration) = GenerateAccessToken(user, permissions);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress);

        token.ReplacedByToken = newRefreshToken.Token;
        await _context.SaveChangesAsync();

        return new AuthResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            accessToken,
            newRefreshToken.Token,
            expiration,
            permissions,
            roles
        );
    }

    public async Task RevokeTokenAsync(string refreshToken, string? ipAddress)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token != null && token.IsActive)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserInfoResponse?> GetUserInfoAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var permissions = await _permissionService.GetUserPermissionsAsync(userId);
        var roles = await _permissionService.GetUserRolesAsync(userId);

        var modules = await _context.Modules
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .Select(m => new ModuleInfo(
                m.Code,
                m.Name,
                m.Icon,
                m.Permissions
                    .Where(p => permissions.Contains(p.Code))
                    .Select(p => p.Code)
                    .ToList()
            ))
            .Where(m => m.Permissions.Any())
            .ToListAsync();

        return new UserInfoResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            permissions,
            roles,
            modules
        );
    }

    private (string token, DateTime expiration) GenerateAccessToken(User user, List<string> permissions)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"]!);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        // Add permissions as claims
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var expiration = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiration);
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(int userId, string? ipAddress)
    {
        var expirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            CreatedByIp = ipAddress
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
