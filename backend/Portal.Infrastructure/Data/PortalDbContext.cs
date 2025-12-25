using Microsoft.EntityFrameworkCore;
using Portal.Core.Entities;

namespace Portal.Infrastructure.Data;

public class PortalDbContext : DbContext
{
    public PortalDbContext(DbContextOptions<PortalDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
        });

        // Module
        modelBuilder.Entity<Module>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        });

        // Permission
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasOne(e => e.Module)
                  .WithMany(m => m.Permissions)
                  .HasForeignKey(e => e.ModuleId);
        });

        // Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        });

        // UserRole (many-to-many)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(e => e.RoleId);
        });

        // RolePermission (many-to-many)
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(e => e.RoleId);
            entity.HasOne(e => e.Permission)
                  .WithMany(p => p.RolePermissions)
                  .HasForeignKey(e => e.PermissionId);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).HasMaxLength(500).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId);
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Modules
        modelBuilder.Entity<Module>().HasData(
            new Module { Id = 1, Code = "IK", Name = "Insan Kaynaklari", Description = "IK islemleri", DisplayOrder = 1 },
            new Module { Id = 2, Code = "BUTCE", Name = "Butce Yonetimi", Description = "Butce islemleri", DisplayOrder = 2 }
        );

        // Permissions
        modelBuilder.Entity<Permission>().HasData(
            // IK Permissions
            new Permission { Id = 1, ModuleId = 1, Code = "IK.Bordro.KendiGoruntule", Name = "Kendi Bordrosunu Goruntule" },
            new Permission { Id = 2, ModuleId = 1, Code = "IK.Bordro.TumGoruntule", Name = "Tum Bordrolari Goruntule" },
            new Permission { Id = 3, ModuleId = 1, Code = "IK.Izin.KendiGoruntule", Name = "Kendi Izinlerini Goruntule" },
            new Permission { Id = 4, ModuleId = 1, Code = "IK.Izin.TumGoruntule", Name = "Tum Izinleri Goruntule" },
            new Permission { Id = 5, ModuleId = 1, Code = "IK.Izin.Onayla", Name = "Izin Onayla" },
            // Butce Permissions
            new Permission { Id = 6, ModuleId = 2, Code = "BUTCE.Kendi.Goruntule", Name = "Kendi Butcesini Goruntule" },
            new Permission { Id = 7, ModuleId = 2, Code = "BUTCE.Tum.Goruntule", Name = "Tum Butceleri Goruntule" },
            new Permission { Id = 8, ModuleId = 2, Code = "BUTCE.Duzenle", Name = "Butce Duzenle" }
        );

        // Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Code = "CALISAN", Name = "Calisan", Description = "Standart calisan" },
            new Role { Id = 2, Code = "IK_YONETICI", Name = "IK Yoneticisi", Description = "IK departmani yoneticisi" },
            new Role { Id = 3, Code = "BUTCE_YONETICI", Name = "Butce Yoneticisi", Description = "Butce yoneticisi" },
            new Role { Id = 4, Code = "ADMIN", Name = "Sistem Yoneticisi", Description = "Tam yetkili kullanici" }
        );

        // RolePermissions
        modelBuilder.Entity<RolePermission>().HasData(
            // CALISAN: kendi bilgilerini gorebilir
            new RolePermission { RoleId = 1, PermissionId = 1 },  // IK.Bordro.KendiGoruntule
            new RolePermission { RoleId = 1, PermissionId = 3 },  // IK.Izin.KendiGoruntule
            new RolePermission { RoleId = 1, PermissionId = 6 },  // BUTCE.Kendi.Goruntule

            // IK_YONETICI: tum IK islemleri
            new RolePermission { RoleId = 2, PermissionId = 1 },
            new RolePermission { RoleId = 2, PermissionId = 2 },
            new RolePermission { RoleId = 2, PermissionId = 3 },
            new RolePermission { RoleId = 2, PermissionId = 4 },
            new RolePermission { RoleId = 2, PermissionId = 5 },
            new RolePermission { RoleId = 2, PermissionId = 6 },

            // BUTCE_YONETICI: tum butce islemleri
            new RolePermission { RoleId = 3, PermissionId = 1 },
            new RolePermission { RoleId = 3, PermissionId = 3 },
            new RolePermission { RoleId = 3, PermissionId = 6 },
            new RolePermission { RoleId = 3, PermissionId = 7 },
            new RolePermission { RoleId = 3, PermissionId = 8 },

            // ADMIN: her sey
            new RolePermission { RoleId = 4, PermissionId = 1 },
            new RolePermission { RoleId = 4, PermissionId = 2 },
            new RolePermission { RoleId = 4, PermissionId = 3 },
            new RolePermission { RoleId = 4, PermissionId = 4 },
            new RolePermission { RoleId = 4, PermissionId = 5 },
            new RolePermission { RoleId = 4, PermissionId = 6 },
            new RolePermission { RoleId = 4, PermissionId = 7 },
            new RolePermission { RoleId = 4, PermissionId = 8 }
        );
    }
}
