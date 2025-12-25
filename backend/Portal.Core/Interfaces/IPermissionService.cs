namespace Portal.Core.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int userId, string permissionCode);
    Task<List<string>> GetUserPermissionsAsync(int userId);
    Task<List<string>> GetUserRolesAsync(int userId);
}
