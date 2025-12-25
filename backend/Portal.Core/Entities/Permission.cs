namespace Portal.Core.Entities;

public class Permission
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string Code { get; set; } = string.Empty;  // IK.Bordro.KendiGoruntule
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation properties
    public Module Module { get; set; } = null!;
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
