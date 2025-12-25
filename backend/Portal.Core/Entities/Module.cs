namespace Portal.Core.Entities;

public class Module
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;  // IK, BUTCE, SATIS
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
