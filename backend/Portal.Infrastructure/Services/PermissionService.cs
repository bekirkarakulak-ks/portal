using Microsoft.EntityFrameworkCore;
using Portal.Core.Interfaces;
using Portal.Infrastructure.Data;

namespace Portal.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly PortalDbContext _context;

    public PermissionService(PortalDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
    {
        var permissions = await GetUserPermissionsAsync(userId);
        return permissions.Contains(permissionCode);
    }

    public async Task<List<string>> GetUserPermissionsAsync(int userId)
    {
        var permissions = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync();

        return permissions;
    }

    public async Task<List<string>> GetUserRolesAsync(int userId)
    {
        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Code)
            .ToListAsync();

        return roles;
    }
}
