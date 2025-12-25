namespace Portal.Core.Entities;

public class Role
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;  // IK_KULLANICI, IK_YONETICI
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
