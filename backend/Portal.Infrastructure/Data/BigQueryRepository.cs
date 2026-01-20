using Google.Cloud.BigQuery.V2;
using Portal.Core.Entities;

namespace Portal.Infrastructure.Data;

public class BigQueryRepository
{
    private readonly BigQueryClient _client;
    private readonly string _projectId;
    private readonly string _datasetId;

    public BigQueryRepository(string projectId, string datasetId)
    {
        _client = BigQueryClient.Create(projectId);
        _projectId = projectId;
        _datasetId = datasetId;
    }

    private string Table(string name) => $"`{_projectId}.{_datasetId}.{name}`";

    // ===== USER OPERATIONS =====
    public async Task<User?> GetUserByUsernameAsync(string username, string client = "00")
    {
        var query = $@"
            SELECT username, passw, name, surname, email, phone, client, company,
                   isblocked, createdat, lastlogintime
            FROM {Table("aridbusers")}
            WHERE LOWER(username) = LOWER(@username) AND client = @client AND isblocked = 0
            LIMIT 1";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("client", BigQueryDbType.String, client)
        };

        var result = await _client.ExecuteQueryAsync(query, parameters);
        var row = result.FirstOrDefault();

        return row == null ? null : MapToUser(row);
    }

    public async Task<User?> GetUserByUsernameOnlyAsync(string username)
    {
        var query = $@"
            SELECT username, passw, name, surname, email, phone, client, company,
                   isblocked, createdat, lastlogintime
            FROM {Table("aridbusers")}
            WHERE LOWER(username) = LOWER(@username) AND isblocked = 0
            LIMIT 1";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username)
        };

        var result = await _client.ExecuteQueryAsync(query, parameters);
        var row = result.FirstOrDefault();

        return row == null ? null : MapToUser(row);
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var query = $@"
            SELECT username, passw, name, surname, email, phone, client, company,
                   isblocked, createdat, lastlogintime
            FROM {Table("aridbusers")}
            ORDER BY createdat DESC";

        var result = await _client.ExecuteQueryAsync(query, null);
        return result.Select(row => MapToUser(row)).ToList();
    }

    public async Task UpdateUserLastLoginAsync(string username, string client)
    {
        var query = $@"
            UPDATE {Table("aridbusers")}
            SET lastlogintime = CURRENT_TIMESTAMP()
            WHERE username = @username AND client = @client";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("client", BigQueryDbType.String, client)
        };

        await _client.ExecuteQueryAsync(query, parameters);
    }

    public async Task UpdateUserAsync(string username, string client, string firstName, string lastName, string email, bool isActive)
    {
        var query = $@"
            UPDATE {Table("aridbusers")}
            SET name = @firstName, surname = @lastName, email = @email, isblocked = @isBlocked
            WHERE username = @username AND client = @client";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("client", BigQueryDbType.String, client),
            new BigQueryParameter("firstName", BigQueryDbType.String, firstName),
            new BigQueryParameter("lastName", BigQueryDbType.String, lastName),
            new BigQueryParameter("email", BigQueryDbType.String, email),
            new BigQueryParameter("isBlocked", BigQueryDbType.Int64, isActive ? 0 : 1)
        };

        await _client.ExecuteQueryAsync(query, parameters);
    }

    public async Task DeleteUserAsync(string username, string client)
    {
        // Soft delete - set isblocked = 1
        var query = $@"
            UPDATE {Table("aridbusers")}
            SET isblocked = 1
            WHERE username = @username AND client = @client";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("client", BigQueryDbType.String, client)
        };

        await _client.ExecuteQueryAsync(query, parameters);
    }

    // ===== REFRESH TOKEN OPERATIONS =====
    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        var query = $@"
            SELECT rt.id, rt.username, rt.client, rt.token, rt.expiresat, rt.createdat,
                   rt.createdbyip, rt.revokedat, rt.revokedbyip, rt.replacedbytoken,
                   u.username as u_username, u.passw, u.name, u.surname, u.email, u.phone,
                   u.client as u_client, u.company, u.isblocked, u.createdat as u_createdat, u.lastlogintime
            FROM {Table("aridbrefreshtokens")} rt
            JOIN {Table("aridbusers")} u ON rt.username = u.username AND rt.client = u.client
            WHERE rt.token = @token
            LIMIT 1";

        var parameters = new[] { new BigQueryParameter("token", BigQueryDbType.String, token) };
        var result = await _client.ExecuteQueryAsync(query, parameters);

        var row = result.FirstOrDefault();
        if (row == null) return null;

        return new RefreshToken
        {
            Id = (int)(long)row["id"],
            UserId = 0,
            Token = (string)row["token"],
            ExpiresAt = row["expiresat"] != null ? ((DateTime)row["expiresat"]).ToUniversalTime() : DateTime.MinValue,
            CreatedAt = row["createdat"] != null ? ((DateTime)row["createdat"]).ToUniversalTime() : DateTime.UtcNow,
            CreatedByIp = row["createdbyip"]?.ToString(),
            RevokedAt = row["revokedat"] != null ? ((DateTime)row["revokedat"]).ToUniversalTime() : null,
            RevokedByIp = row["revokedbyip"]?.ToString(),
            ReplacedByToken = row["replacedbytoken"]?.ToString(),
            User = MapToUser(row, "u_")
        };
    }

    public async Task<int> CreateRefreshTokenAsync(string username, string client, string token, DateTime expiresAt, string? ipAddress)
    {
        var maxIdQuery = $"SELECT COALESCE(MAX(id), 0) + 1 as NextId FROM {Table("aridbrefreshtokens")}";
        var maxIdResult = await _client.ExecuteQueryAsync(maxIdQuery, null);
        var nextId = (int)(long)maxIdResult.First()["NextId"];

        var query = $@"
            INSERT INTO {Table("aridbrefreshtokens")}
            (id, username, client, token, expiresat, createdat, createdbyip)
            VALUES (@id, @username, @client, @token, @expiresat, CURRENT_TIMESTAMP(), @ip)";

        var parameters = new[]
        {
            new BigQueryParameter("id", BigQueryDbType.Int64, nextId),
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("client", BigQueryDbType.String, client),
            new BigQueryParameter("token", BigQueryDbType.String, token),
            new BigQueryParameter("expiresat", BigQueryDbType.Timestamp, expiresAt),
            new BigQueryParameter("ip", BigQueryDbType.String, ipAddress)
        };

        await _client.ExecuteQueryAsync(query, parameters);
        return nextId;
    }

    public async Task RevokeRefreshTokenAsync(string token, string? ipAddress, string? replacedByToken = null)
    {
        var query = $@"
            UPDATE {Table("aridbrefreshtokens")}
            SET revokedat = CURRENT_TIMESTAMP(), revokedbyip = @ip, replacedbytoken = @replacedby
            WHERE token = @token";

        var parameters = new[]
        {
            new BigQueryParameter("token", BigQueryDbType.String, token),
            new BigQueryParameter("ip", BigQueryDbType.String, ipAddress),
            new BigQueryParameter("replacedby", BigQueryDbType.String, replacedByToken)
        };

        await _client.ExecuteQueryAsync(query, parameters);
    }

    // ===== USER CREATION =====
    public async Task<bool> UserExistsAsync(string username, string? email = null, string client = "00")
    {
        var query = $@"
            SELECT 1
            FROM {Table("aridbusers")}
            WHERE (LOWER(username) = LOWER(@username) AND client = @client)
               OR (email IS NOT NULL AND LOWER(email) = LOWER(@email))
            LIMIT 1";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("client", BigQueryDbType.String, client),
            new BigQueryParameter("email", BigQueryDbType.String, email ?? "")
        };

        var result = await _client.ExecuteQueryAsync(query, parameters);
        return result.Any();
    }

    public async Task<User> CreateUserAsync(string username, string passwordHash, string firstName,
        string lastName, string email, string? phone = null, string client = "00")
    {
        var query = $@"
            INSERT INTO {Table("aridbusers")}
            (client, username, passw, name, surname, email, phone, isblocked, createdat)
            VALUES (@client, @username, @passw, @name, @surname, @email, @phone, 0, CURRENT_TIMESTAMP())";

        var parameters = new[]
        {
            new BigQueryParameter("client", BigQueryDbType.String, client),
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("passw", BigQueryDbType.String, passwordHash),
            new BigQueryParameter("name", BigQueryDbType.String, firstName),
            new BigQueryParameter("surname", BigQueryDbType.String, lastName),
            new BigQueryParameter("email", BigQueryDbType.String, email),
            new BigQueryParameter("phone", BigQueryDbType.String, phone)
        };

        await _client.ExecuteQueryAsync(query, parameters);

        return new User
        {
            Username = username,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            Client = client,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ===== ORGANIZATION OPERATIONS =====
    public async Task<Organization?> GetOrganizationByEmailAsync(string email)
    {
        var query = $@"
            SELECT Id, EmailPattern, DepartmentCode, DepartmentName, DefaultRoleId, Priority, IsActive
            FROM {Table("Organization")}
            WHERE IsActive = TRUE AND @email LIKE EmailPattern
            ORDER BY Priority DESC
            LIMIT 1";

        var parameters = new[] { new BigQueryParameter("email", BigQueryDbType.String, email) };
        var result = await _client.ExecuteQueryAsync(query, parameters);
        var row = result.FirstOrDefault();

        if (row == null) return null;

        return new Organization
        {
            Id = (int)(long)row["Id"],
            EmailPattern = row["EmailPattern"]?.ToString() ?? "",
            DepartmentCode = row["DepartmentCode"]?.ToString() ?? "",
            DepartmentName = row["DepartmentName"]?.ToString() ?? "",
            DefaultRoleId = (int)(long)row["DefaultRoleId"],
            Priority = (int)(long)row["Priority"],
            IsActive = (bool)row["IsActive"]
        };
    }

    public async Task<List<Organization>> GetAllOrganizationsAsync()
    {
        var query = $@"
            SELECT o.Id, o.EmailPattern, o.DepartmentCode, o.DepartmentName, o.DefaultRoleId, o.Priority, o.IsActive,
                   r.Code as RoleCode, r.Name as RoleName
            FROM {Table("Organization")} o
            LEFT JOIN {Table("Roles")} r ON o.DefaultRoleId = r.Id
            ORDER BY o.Priority DESC";

        var result = await _client.ExecuteQueryAsync(query, null);
        return result.Select(row => new Organization
        {
            Id = (int)(long)row["Id"],
            EmailPattern = row["EmailPattern"]?.ToString() ?? "",
            DepartmentCode = row["DepartmentCode"]?.ToString() ?? "",
            DepartmentName = row["DepartmentName"]?.ToString() ?? "",
            DefaultRoleId = (int)(long)row["DefaultRoleId"],
            Priority = (int)(long)row["Priority"],
            IsActive = (bool)row["IsActive"]
        }).ToList();
    }

    public async Task<Organization> CreateOrganizationAsync(Organization org)
    {
        var maxIdQuery = $"SELECT COALESCE(MAX(Id), 0) + 1 as NextId FROM {Table("Organization")}";
        var maxIdResult = await _client.ExecuteQueryAsync(maxIdQuery, null);
        var nextId = (int)(long)maxIdResult.First()["NextId"];

        var query = $@"
            INSERT INTO {Table("Organization")}
            (Id, EmailPattern, DepartmentCode, DepartmentName, DefaultRoleId, Priority, IsActive)
            VALUES (@id, @pattern, @deptCode, @deptName, @roleId, @priority, @isActive)";

        var parameters = new[]
        {
            new BigQueryParameter("id", BigQueryDbType.Int64, nextId),
            new BigQueryParameter("pattern", BigQueryDbType.String, org.EmailPattern),
            new BigQueryParameter("deptCode", BigQueryDbType.String, org.DepartmentCode),
            new BigQueryParameter("deptName", BigQueryDbType.String, org.DepartmentName),
            new BigQueryParameter("roleId", BigQueryDbType.Int64, org.DefaultRoleId),
            new BigQueryParameter("priority", BigQueryDbType.Int64, org.Priority),
            new BigQueryParameter("isActive", BigQueryDbType.Bool, org.IsActive)
        };

        await _client.ExecuteQueryAsync(query, parameters);
        org.Id = nextId;
        return org;
    }

    public async Task UpdateOrganizationAsync(Organization org)
    {
        var query = $@"
            UPDATE {Table("Organization")}
            SET EmailPattern = @pattern, DepartmentCode = @deptCode, DepartmentName = @deptName,
                DefaultRoleId = @roleId, Priority = @priority, IsActive = @isActive
            WHERE Id = @id";

        var parameters = new[]
        {
            new BigQueryParameter("id", BigQueryDbType.Int64, org.Id),
            new BigQueryParameter("pattern", BigQueryDbType.String, org.EmailPattern),
            new BigQueryParameter("deptCode", BigQueryDbType.String, org.DepartmentCode),
            new BigQueryParameter("deptName", BigQueryDbType.String, org.DepartmentName),
            new BigQueryParameter("roleId", BigQueryDbType.Int64, org.DefaultRoleId),
            new BigQueryParameter("priority", BigQueryDbType.Int64, org.Priority),
            new BigQueryParameter("isActive", BigQueryDbType.Bool, org.IsActive)
        };

        await _client.ExecuteQueryAsync(query, parameters);
    }

    public async Task DeleteOrganizationAsync(int id)
    {
        var query = $"UPDATE {Table("Organization")} SET IsActive = FALSE WHERE Id = @id";
        var parameters = new[] { new BigQueryParameter("id", BigQueryDbType.Int64, id) };
        await _client.ExecuteQueryAsync(query, parameters);
    }

    // ===== ROLE OPERATIONS =====
    public async Task<List<string>> GetUserRolesAsync(string username, string client)
    {
        var query = $@"
            SELECT r.Code
            FROM {Table("UserRoles")} ur
            JOIN {Table("Roles")} r ON ur.RoleId = r.Id
            JOIN {Table("aridbusers")} u ON ur.UserId = u.username
            WHERE u.username = @username AND u.client = @client AND r.IsActive = TRUE";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("client", BigQueryDbType.String, client)
        };

        try
        {
            var result = await _client.ExecuteQueryAsync(query, parameters);
            return result.Select(row => row["Code"]?.ToString() ?? "").Where(c => !string.IsNullOrEmpty(c)).ToList();
        }
        catch
        {
            // Tables not yet created, return default
            return new List<string> { "CALISAN" };
        }
    }

    public async Task<List<string>> GetUserPermissionsAsync(string username, string client)
    {
        var query = $@"
            SELECT DISTINCT p.Code
            FROM {Table("UserRoles")} ur
            JOIN {Table("RolePermissions")} rp ON ur.RoleId = rp.RoleId
            JOIN {Table("Permissions")} p ON rp.PermissionId = p.Id
            JOIN {Table("aridbusers")} u ON ur.UserId = u.username
            WHERE u.username = @username AND u.client = @client";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("client", BigQueryDbType.String, client)
        };

        try
        {
            var result = await _client.ExecuteQueryAsync(query, parameters);
            var permissions = result.Select(row => row["Code"]?.ToString() ?? "").Where(c => !string.IsNullOrEmpty(c)).ToList();

            // Return default permissions if none found
            if (!permissions.Any())
            {
                return new List<string>
                {
                    "IK.Bordro.Kendi",
                    "IK.Izin.Kendi",
                    "Butce.Kendi"
                };
            }
            return permissions;
        }
        catch
        {
            // Tables not yet created, return default
            return new List<string>
            {
                "IK.Bordro.Kendi",
                "IK.Izin.Kendi",
                "Butce.Kendi"
            };
        }
    }

    public async Task<List<Role>> GetAllRolesAsync()
    {
        var query = $@"
            SELECT Id, Code, Name, Description, IsActive
            FROM {Table("Roles")}
            WHERE IsActive = TRUE
            ORDER BY Name";

        var result = await _client.ExecuteQueryAsync(query, null);
        return result.Select(row => new Role
        {
            Id = (int)(long)row["Id"],
            Code = row["Code"]?.ToString() ?? "",
            Name = row["Name"]?.ToString() ?? "",
            Description = row["Description"]?.ToString(),
            IsActive = (bool)row["IsActive"]
        }).ToList();
    }

    public async Task<Role?> GetRoleByIdAsync(int id)
    {
        var query = $@"
            SELECT Id, Code, Name, Description, IsActive
            FROM {Table("Roles")}
            WHERE Id = @id";

        var parameters = new[] { new BigQueryParameter("id", BigQueryDbType.Int64, id) };
        var result = await _client.ExecuteQueryAsync(query, parameters);
        var row = result.FirstOrDefault();

        if (row == null) return null;

        return new Role
        {
            Id = (int)(long)row["Id"],
            Code = row["Code"]?.ToString() ?? "",
            Name = row["Name"]?.ToString() ?? "",
            Description = row["Description"]?.ToString(),
            IsActive = (bool)row["IsActive"]
        };
    }

    public async Task<Role> CreateRoleAsync(Role role)
    {
        var maxIdQuery = $"SELECT COALESCE(MAX(Id), 0) + 1 as NextId FROM {Table("Roles")}";
        var maxIdResult = await _client.ExecuteQueryAsync(maxIdQuery, null);
        var nextId = (int)(long)maxIdResult.First()["NextId"];

        var query = $@"
            INSERT INTO {Table("Roles")} (Id, Code, Name, Description, IsActive)
            VALUES (@id, @code, @name, @desc, @isActive)";

        var parameters = new[]
        {
            new BigQueryParameter("id", BigQueryDbType.Int64, nextId),
            new BigQueryParameter("code", BigQueryDbType.String, role.Code),
            new BigQueryParameter("name", BigQueryDbType.String, role.Name),
            new BigQueryParameter("desc", BigQueryDbType.String, role.Description),
            new BigQueryParameter("isActive", BigQueryDbType.Bool, role.IsActive)
        };

        await _client.ExecuteQueryAsync(query, parameters);
        role.Id = nextId;
        return role;
    }

    public async Task UpdateRoleAsync(Role role)
    {
        var query = $@"
            UPDATE {Table("Roles")}
            SET Code = @code, Name = @name, Description = @desc, IsActive = @isActive
            WHERE Id = @id";

        var parameters = new[]
        {
            new BigQueryParameter("id", BigQueryDbType.Int64, role.Id),
            new BigQueryParameter("code", BigQueryDbType.String, role.Code),
            new BigQueryParameter("name", BigQueryDbType.String, role.Name),
            new BigQueryParameter("desc", BigQueryDbType.String, role.Description),
            new BigQueryParameter("isActive", BigQueryDbType.Bool, role.IsActive)
        };

        await _client.ExecuteQueryAsync(query, parameters);
    }

    public async Task DeleteRoleAsync(int id)
    {
        var query = $"UPDATE {Table("Roles")} SET IsActive = FALSE WHERE Id = @id";
        var parameters = new[] { new BigQueryParameter("id", BigQueryDbType.Int64, id) };
        await _client.ExecuteQueryAsync(query, parameters);
    }

    // ===== USER-ROLE OPERATIONS =====
    public async Task<List<int>> GetUserRoleIdsAsync(string username, string client)
    {
        var query = $@"
            SELECT ur.RoleId
            FROM {Table("UserRoles")} ur
            JOIN {Table("aridbusers")} u ON ur.UserId = u.username
            WHERE u.username = @username AND u.client = @client";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("client", BigQueryDbType.String, client)
        };

        try
        {
            var result = await _client.ExecuteQueryAsync(query, parameters);
            return result.Select(row => (int)(long)row["RoleId"]).ToList();
        }
        catch
        {
            return new List<int>();
        }
    }

    public async Task AssignRoleToUserAsync(string username, int roleId, int? assignedBy = null)
    {
        var query = $@"
            INSERT INTO {Table("UserRoles")} (UserId, RoleId, AssignedAt, AssignedBy)
            VALUES (@username, @roleId, CURRENT_TIMESTAMP(), @assignedBy)";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("roleId", BigQueryDbType.Int64, roleId),
            new BigQueryParameter("assignedBy", BigQueryDbType.Int64, assignedBy)
        };

        await _client.ExecuteQueryAsync(query, parameters);
    }

    public async Task RemoveRoleFromUserAsync(string username, int roleId)
    {
        var query = $@"
            DELETE FROM {Table("UserRoles")}
            WHERE UserId = @username AND RoleId = @roleId";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("roleId", BigQueryDbType.Int64, roleId)
        };

        await _client.ExecuteQueryAsync(query, parameters);
    }

    public async Task UpdateUserRolesAsync(string username, List<int> roleIds, int? assignedBy = null)
    {
        // Remove all existing roles
        var deleteQuery = $"DELETE FROM {Table("UserRoles")} WHERE UserId = @username";
        var deleteParams = new[] { new BigQueryParameter("username", BigQueryDbType.String, username) };
        await _client.ExecuteQueryAsync(deleteQuery, deleteParams);

        // Add new roles
        foreach (var roleId in roleIds)
        {
            await AssignRoleToUserAsync(username, roleId, assignedBy);
        }
    }

    // ===== PERMISSION OPERATIONS =====
    public async Task<List<Permission>> GetAllPermissionsAsync()
    {
        var query = $@"
            SELECT p.Id, p.SubModuleId, p.LevelId, p.Code, p.Name, p.Description,
                   sm.Code as SubModuleCode, sm.Name as SubModuleName, sm.ModuleId,
                   m.Code as ModuleCode, m.Name as ModuleName,
                   pl.Level, pl.Code as LevelCode, pl.Name as LevelName
            FROM {Table("Permissions")} p
            JOIN {Table("SubModules")} sm ON p.SubModuleId = sm.Id
            JOIN {Table("Modules")} m ON sm.ModuleId = m.Id
            JOIN {Table("PermissionLevels")} pl ON p.LevelId = pl.Id
            ORDER BY m.DisplayOrder, sm.DisplayOrder, pl.Level";

        var result = await _client.ExecuteQueryAsync(query, null);
        return result.Select(row => new Permission
        {
            Id = (int)(long)row["Id"],
            SubModuleId = (int)(long)row["SubModuleId"],
            LevelId = (int)(long)row["LevelId"],
            Code = row["Code"]?.ToString() ?? "",
            Name = row["Name"]?.ToString() ?? "",
            Description = row["Description"]?.ToString()
        }).ToList();
    }

    public async Task<List<int>> GetRolePermissionIdsAsync(int roleId)
    {
        var query = $@"
            SELECT PermissionId
            FROM {Table("RolePermissions")}
            WHERE RoleId = @roleId";

        var parameters = new[] { new BigQueryParameter("roleId", BigQueryDbType.Int64, roleId) };
        var result = await _client.ExecuteQueryAsync(query, parameters);
        return result.Select(row => (int)(long)row["PermissionId"]).ToList();
    }

    public async Task UpdateRolePermissionsAsync(int roleId, List<int> permissionIds)
    {
        // Remove all existing permissions
        var deleteQuery = $"DELETE FROM {Table("RolePermissions")} WHERE RoleId = @roleId";
        var deleteParams = new[] { new BigQueryParameter("roleId", BigQueryDbType.Int64, roleId) };
        await _client.ExecuteQueryAsync(deleteQuery, deleteParams);

        // Add new permissions
        foreach (var permissionId in permissionIds)
        {
            var insertQuery = $@"
                INSERT INTO {Table("RolePermissions")} (RoleId, PermissionId)
                VALUES (@roleId, @permissionId)";

            var insertParams = new[]
            {
                new BigQueryParameter("roleId", BigQueryDbType.Int64, roleId),
                new BigQueryParameter("permissionId", BigQueryDbType.Int64, permissionId)
            };

            await _client.ExecuteQueryAsync(insertQuery, insertParams);
        }
    }

    // ===== MODULE OPERATIONS =====
    public async Task<List<Module>> GetAllModulesAsync()
    {
        var query = $@"
            SELECT Id, Code, Name, Description, Icon, DisplayOrder, IsActive
            FROM {Table("Modules")}
            WHERE IsActive = TRUE
            ORDER BY DisplayOrder";

        var result = await _client.ExecuteQueryAsync(query, null);
        return result.Select(row => new Module
        {
            Id = (int)(long)row["Id"],
            Code = row["Code"]?.ToString() ?? "",
            Name = row["Name"]?.ToString() ?? "",
            Description = row["Description"]?.ToString(),
            Icon = row["Icon"]?.ToString(),
            DisplayOrder = (int)(long)row["DisplayOrder"],
            IsActive = (bool)row["IsActive"]
        }).ToList();
    }

    public async Task<List<SubModule>> GetAllSubModulesAsync()
    {
        var query = $@"
            SELECT Id, ModuleId, Code, Name, Description, DisplayOrder, IsActive
            FROM {Table("SubModules")}
            WHERE IsActive = TRUE
            ORDER BY ModuleId, DisplayOrder";

        var result = await _client.ExecuteQueryAsync(query, null);
        return result.Select(row => new SubModule
        {
            Id = (int)(long)row["Id"],
            ModuleId = (int)(long)row["ModuleId"],
            Code = row["Code"]?.ToString() ?? "",
            Name = row["Name"]?.ToString() ?? "",
            Description = row["Description"]?.ToString(),
            DisplayOrder = (int)(long)row["DisplayOrder"],
            IsActive = (bool)row["IsActive"]
        }).ToList();
    }

    public async Task<List<PermissionLevel>> GetAllPermissionLevelsAsync()
    {
        var query = $@"
            SELECT Id, Level, Code, Name, Description
            FROM {Table("PermissionLevels")}
            ORDER BY Level";

        var result = await _client.ExecuteQueryAsync(query, null);
        return result.Select(row => new PermissionLevel
        {
            Id = (int)(long)row["Id"],
            Level = (int)(long)row["Level"],
            Code = row["Code"]?.ToString() ?? "",
            Name = row["Name"]?.ToString() ?? "",
            Description = row["Description"]?.ToString()
        }).ToList();
    }

    public async Task<Module> CreateModuleAsync(Module module)
    {
        var maxIdQuery = $"SELECT COALESCE(MAX(Id), 0) + 1 as NextId FROM {Table("Modules")}";
        var maxIdResult = await _client.ExecuteQueryAsync(maxIdQuery, null);
        var nextId = (int)(long)maxIdResult.First()["NextId"];

        var query = $@"
            INSERT INTO {Table("Modules")} (Id, Code, Name, Description, Icon, DisplayOrder, IsActive)
            VALUES (@id, @code, @name, @desc, @icon, @order, @isActive)";

        var parameters = new[]
        {
            new BigQueryParameter("id", BigQueryDbType.Int64, nextId),
            new BigQueryParameter("code", BigQueryDbType.String, module.Code),
            new BigQueryParameter("name", BigQueryDbType.String, module.Name),
            new BigQueryParameter("desc", BigQueryDbType.String, module.Description),
            new BigQueryParameter("icon", BigQueryDbType.String, module.Icon),
            new BigQueryParameter("order", BigQueryDbType.Int64, module.DisplayOrder),
            new BigQueryParameter("isActive", BigQueryDbType.Bool, module.IsActive)
        };

        await _client.ExecuteQueryAsync(query, parameters);
        module.Id = nextId;
        return module;
    }

    public async Task UpdateModuleAsync(Module module)
    {
        var query = $@"
            UPDATE {Table("Modules")}
            SET Code = @code, Name = @name, Description = @desc, Icon = @icon,
                DisplayOrder = @order, IsActive = @isActive
            WHERE Id = @id";

        var parameters = new[]
        {
            new BigQueryParameter("id", BigQueryDbType.Int64, module.Id),
            new BigQueryParameter("code", BigQueryDbType.String, module.Code),
            new BigQueryParameter("name", BigQueryDbType.String, module.Name),
            new BigQueryParameter("desc", BigQueryDbType.String, module.Description),
            new BigQueryParameter("icon", BigQueryDbType.String, module.Icon),
            new BigQueryParameter("order", BigQueryDbType.Int64, module.DisplayOrder),
            new BigQueryParameter("isActive", BigQueryDbType.Bool, module.IsActive)
        };

        await _client.ExecuteQueryAsync(query, parameters);
    }

    public async Task<SubModule> CreateSubModuleAsync(SubModule subModule)
    {
        var maxIdQuery = $"SELECT COALESCE(MAX(Id), 0) + 1 as NextId FROM {Table("SubModules")}";
        var maxIdResult = await _client.ExecuteQueryAsync(maxIdQuery, null);
        var nextId = (int)(long)maxIdResult.First()["NextId"];

        var query = $@"
            INSERT INTO {Table("SubModules")} (Id, ModuleId, Code, Name, Description, DisplayOrder, IsActive)
            VALUES (@id, @moduleId, @code, @name, @desc, @order, @isActive)";

        var parameters = new[]
        {
            new BigQueryParameter("id", BigQueryDbType.Int64, nextId),
            new BigQueryParameter("moduleId", BigQueryDbType.Int64, subModule.ModuleId),
            new BigQueryParameter("code", BigQueryDbType.String, subModule.Code),
            new BigQueryParameter("name", BigQueryDbType.String, subModule.Name),
            new BigQueryParameter("desc", BigQueryDbType.String, subModule.Description),
            new BigQueryParameter("order", BigQueryDbType.Int64, subModule.DisplayOrder),
            new BigQueryParameter("isActive", BigQueryDbType.Bool, subModule.IsActive)
        };

        await _client.ExecuteQueryAsync(query, parameters);
        subModule.Id = nextId;
        return subModule;
    }

    public async Task UpdateSubModuleAsync(SubModule subModule)
    {
        var query = $@"
            UPDATE {Table("SubModules")}
            SET ModuleId = @moduleId, Code = @code, Name = @name, Description = @desc,
                DisplayOrder = @order, IsActive = @isActive
            WHERE Id = @id";

        var parameters = new[]
        {
            new BigQueryParameter("id", BigQueryDbType.Int64, subModule.Id),
            new BigQueryParameter("moduleId", BigQueryDbType.Int64, subModule.ModuleId),
            new BigQueryParameter("code", BigQueryDbType.String, subModule.Code),
            new BigQueryParameter("name", BigQueryDbType.String, subModule.Name),
            new BigQueryParameter("desc", BigQueryDbType.String, subModule.Description),
            new BigQueryParameter("order", BigQueryDbType.Int64, subModule.DisplayOrder),
            new BigQueryParameter("isActive", BigQueryDbType.Bool, subModule.IsActive)
        };

        await _client.ExecuteQueryAsync(query, parameters);
    }

    // ===== ORGANIZATION EMPLOYEE LOOKUP =====
    /// <summary>
    /// Organizasyon sorgusundan email adresine gore calisan bilgisi getirir
    /// Canias ERP tablolarindan join ile cekilir
    /// </summary>
    public async Task<OrganizationEmployee?> GetEmployeeByEmailAsync(string email)
    {
        var query = $@"
            SELECT
                BC.NAME,
                BC.SURNAME,
                BC.EMAIL,
                X.STEXT AS DEPARTMAN,
                SBX.STEXT AS GOREV,
                SDX.STEXT AS UNVAN,
                O.CONTACTNUM
            FROM `{_projectId}.{_datasetId}.iasadrbkcntorg` O
            INNER JOIN `{_projectId}.{_datasetId}.iasbas082x` X
                ON O.CLIENT = X.CLIENT
                AND O.PLANT = X.PLANT
                AND O.DEPARTMENT = X.DEPARTCODE
                AND X.LANGU = 'T'
                AND X.COMPANY = '01'
            INNER JOIN `{_projectId}.{_datasetId}.iasbas081` SB
                ON O.CLIENT = SB.CLIENT
                AND O.ORGPLACE = SB.ORGPLACE
                AND SB.COMPANY = '01'
            INNER JOIN `{_projectId}.{_datasetId}.iasbas081x` SBX
                ON SBX.CLIENT = SB.CLIENT
                AND SBX.COMPANY = SB.COMPANY
                AND SBX.ORGPLACE = SB.ORGPLACE
                AND SBX.LANGU = 'T'
            INNER JOIN `{_projectId}.{_datasetId}.iasbas084x` SDX
                ON O.CLIENT = SB.CLIENT
                AND SDX.COMPANY = '01'
                AND SDX.DEPARTCODE = O.DEPARTMENT
                AND SDX.WORKTITLE = O.WORKTITLE
                AND SDX.PLANT = O.PLANT
                AND SDX.LANGU = 'T'
            INNER JOIN `{_projectId}.{_datasetId}.iasadrbookcontact` BC
                ON O.CLIENT = BC.CLIENT
                AND O.CONTACTNUM = BC.CONTACTNUM
            WHERE O.VALIDUNTIL = '2100-01-01'
                AND LOWER(BC.EMAIL) = LOWER(@email)
            LIMIT 1";

        var parameters = new[] { new BigQueryParameter("email", BigQueryDbType.String, email) };

        try
        {
            var result = await _client.ExecuteQueryAsync(query, parameters);
            var row = result.FirstOrDefault();

            if (row == null) return null;

            return new OrganizationEmployee
            {
                Name = row["NAME"]?.ToString() ?? "",
                Surname = row["SURNAME"]?.ToString() ?? "",
                Email = row["EMAIL"]?.ToString() ?? "",
                Department = row["DEPARTMAN"]?.ToString() ?? "",
                Position = row["GOREV"]?.ToString() ?? "",
                Title = row["UNVAN"]?.ToString() ?? "",
                Phone = row["CONTACTNUM"]?.ToString()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not query organization employee: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Tum organizasyon calisanlarinin email listesini getirir
    /// Canias ERP tablolarindan join ile cekilir
    /// </summary>
    public async Task<List<string>> GetAllOrganizationEmailsAsync()
    {
        var query = $@"
            SELECT DISTINCT LOWER(BC.EMAIL) as EMAIL
            FROM `{_projectId}.{_datasetId}.iasadrbkcntorg` O
            INNER JOIN `{_projectId}.{_datasetId}.iasadrbookcontact` BC
                ON O.CLIENT = BC.CLIENT
                AND O.CONTACTNUM = BC.CONTACTNUM
            WHERE O.VALIDUNTIL = '2100-01-01'
                AND BC.EMAIL IS NOT NULL
                AND BC.EMAIL != ''";

        try
        {
            var result = await _client.ExecuteQueryAsync(query, null);
            return result.Select(row => row["EMAIL"]?.ToString() ?? "").Where(e => !string.IsNullOrEmpty(e)).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    // ===== EMAIL VERIFICATION TOKEN OPERATIONS =====
    public async Task<EmailVerificationToken> CreateEmailVerificationTokenAsync(string username, string email, string token, DateTime expiresAt)
    {
        var maxIdQuery = $"SELECT COALESCE(MAX(Id), 0) + 1 as NextId FROM {Table("EmailVerificationTokens")}";
        var maxIdResult = await _client.ExecuteQueryAsync(maxIdQuery, null);
        var nextId = (int)(long)maxIdResult.First()["NextId"];

        var query = $@"
            INSERT INTO {Table("EmailVerificationTokens")}
            (Id, Username, Email, Token, ExpiresAt, CreatedAt, IsUsed)
            VALUES (@id, @username, @email, @token, @expiresAt, CURRENT_TIMESTAMP(), FALSE)";

        var parameters = new[]
        {
            new BigQueryParameter("id", BigQueryDbType.Int64, nextId),
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("email", BigQueryDbType.String, email),
            new BigQueryParameter("token", BigQueryDbType.String, token),
            new BigQueryParameter("expiresAt", BigQueryDbType.Timestamp, expiresAt)
        };

        await _client.ExecuteQueryAsync(query, parameters);

        return new EmailVerificationToken
        {
            Id = nextId,
            Username = username,
            Email = email,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };
    }

    public async Task<EmailVerificationToken?> GetEmailVerificationTokenAsync(string token)
    {
        var query = $@"
            SELECT Id, Username, Email, Token, ExpiresAt, CreatedAt, IsUsed, UsedAt
            FROM {Table("EmailVerificationTokens")}
            WHERE Token = @token
            LIMIT 1";

        var parameters = new[] { new BigQueryParameter("token", BigQueryDbType.String, token) };
        var result = await _client.ExecuteQueryAsync(query, parameters);
        var row = result.FirstOrDefault();

        if (row == null) return null;

        return new EmailVerificationToken
        {
            Id = (int)(long)row["Id"],
            Username = row["Username"]?.ToString() ?? "",
            Email = row["Email"]?.ToString() ?? "",
            Token = row["Token"]?.ToString() ?? "",
            ExpiresAt = row["ExpiresAt"] != null ? ((DateTime)row["ExpiresAt"]).ToUniversalTime() : DateTime.MinValue,
            CreatedAt = row["CreatedAt"] != null ? ((DateTime)row["CreatedAt"]).ToUniversalTime() : DateTime.UtcNow,
            IsUsed = row["IsUsed"] != null && (bool)row["IsUsed"],
            UsedAt = row["UsedAt"] != null ? ((DateTime)row["UsedAt"]).ToUniversalTime() : null
        };
    }

    public async Task MarkEmailVerificationTokenUsedAsync(string token)
    {
        var query = $@"
            UPDATE {Table("EmailVerificationTokens")}
            SET IsUsed = TRUE, UsedAt = CURRENT_TIMESTAMP()
            WHERE Token = @token";

        var parameters = new[] { new BigQueryParameter("token", BigQueryDbType.String, token) };
        await _client.ExecuteQueryAsync(query, parameters);
    }

    public async Task VerifyUserEmailAsync(string username, string client)
    {
        var query = $@"
            UPDATE {Table("aridbusers")}
            SET isblocked = 0
            WHERE username = @username AND client = @client";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("client", BigQueryDbType.String, client)
        };

        await _client.ExecuteQueryAsync(query, parameters);
    }

    // ===== USER CREATION (Updated for pending verification) =====
    public async Task<User> CreateUserPendingVerificationAsync(string username, string passwordHash, string firstName,
        string lastName, string email, string? phone = null, string client = "00")
    {
        // isblocked = 1 (pending verification)
        var query = $@"
            INSERT INTO {Table("aridbusers")}
            (client, username, passw, name, surname, email, phone, isblocked, createdat)
            VALUES (@client, @username, @passw, @name, @surname, @email, @phone, 1, CURRENT_TIMESTAMP())";

        var parameters = new[]
        {
            new BigQueryParameter("client", BigQueryDbType.String, client),
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("passw", BigQueryDbType.String, passwordHash),
            new BigQueryParameter("name", BigQueryDbType.String, firstName),
            new BigQueryParameter("surname", BigQueryDbType.String, lastName),
            new BigQueryParameter("email", BigQueryDbType.String, email),
            new BigQueryParameter("phone", BigQueryDbType.String, phone)
        };

        await _client.ExecuteQueryAsync(query, parameters);

        return new User
        {
            Username = username,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            Client = client,
            IsActive = false, // Pending verification
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Kullanici var mi kontrol et (dogrulama bekleyen dahil)
    /// </summary>
    public async Task<bool> UserExistsIncludingPendingAsync(string username, string? email = null, string client = "00")
    {
        var query = $@"
            SELECT 1
            FROM {Table("aridbusers")}
            WHERE (LOWER(username) = LOWER(@username) AND client = @client)
               OR (email IS NOT NULL AND LOWER(email) = LOWER(@email))
            LIMIT 1";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("client", BigQueryDbType.String, client),
            new BigQueryParameter("email", BigQueryDbType.String, email ?? "")
        };

        var result = await _client.ExecuteQueryAsync(query, parameters);
        return result.Any();
    }

    // ===== PASSWORD RESET TOKEN OPERATIONS =====
    public async Task<PasswordResetToken> CreatePasswordResetTokenAsync(string username, string email, string token, DateTime expiresAt)
    {
        var maxIdQuery = $"SELECT COALESCE(MAX(Id), 0) + 1 as NextId FROM {Table("PasswordResetTokens")}";
        var maxIdResult = await _client.ExecuteQueryAsync(maxIdQuery, null);
        var nextId = (int)(long)maxIdResult.First()["NextId"];

        var query = $@"
            INSERT INTO {Table("PasswordResetTokens")}
            (Id, Username, Email, Token, ExpiresAt, CreatedAt, IsUsed)
            VALUES (@id, @username, @email, @token, @expiresAt, CURRENT_TIMESTAMP(), FALSE)";

        var parameters = new[]
        {
            new BigQueryParameter("id", BigQueryDbType.Int64, nextId),
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("email", BigQueryDbType.String, email),
            new BigQueryParameter("token", BigQueryDbType.String, token),
            new BigQueryParameter("expiresAt", BigQueryDbType.Timestamp, expiresAt)
        };

        await _client.ExecuteQueryAsync(query, parameters);

        return new PasswordResetToken
        {
            Id = nextId,
            Username = username,
            Email = email,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };
    }

    public async Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token)
    {
        var query = $@"
            SELECT Id, Username, Email, Token, ExpiresAt, CreatedAt, IsUsed, UsedAt
            FROM {Table("PasswordResetTokens")}
            WHERE Token = @token
            LIMIT 1";

        var parameters = new[] { new BigQueryParameter("token", BigQueryDbType.String, token) };
        var result = await _client.ExecuteQueryAsync(query, parameters);
        var row = result.FirstOrDefault();

        if (row == null) return null;

        return new PasswordResetToken
        {
            Id = (int)(long)row["Id"],
            Username = row["Username"]?.ToString() ?? "",
            Email = row["Email"]?.ToString() ?? "",
            Token = row["Token"]?.ToString() ?? "",
            ExpiresAt = row["ExpiresAt"] != null ? ((DateTime)row["ExpiresAt"]).ToUniversalTime() : DateTime.MinValue,
            CreatedAt = row["CreatedAt"] != null ? ((DateTime)row["CreatedAt"]).ToUniversalTime() : DateTime.UtcNow,
            IsUsed = row["IsUsed"] != null && (bool)row["IsUsed"],
            UsedAt = row["UsedAt"] != null ? ((DateTime)row["UsedAt"]).ToUniversalTime() : null
        };
    }

    public async Task MarkPasswordResetTokenUsedAsync(string token)
    {
        var query = $@"
            UPDATE {Table("PasswordResetTokens")}
            SET IsUsed = TRUE, UsedAt = CURRENT_TIMESTAMP()
            WHERE Token = @token";

        var parameters = new[] { new BigQueryParameter("token", BigQueryDbType.String, token) };
        await _client.ExecuteQueryAsync(query, parameters);
    }

    public async Task InvalidateUserPasswordResetTokensAsync(string username)
    {
        var query = $@"
            UPDATE {Table("PasswordResetTokens")}
            SET IsUsed = TRUE, UsedAt = CURRENT_TIMESTAMP()
            WHERE Username = @username AND IsUsed = FALSE";

        var parameters = new[] { new BigQueryParameter("username", BigQueryDbType.String, username) };
        await _client.ExecuteQueryAsync(query, parameters);
    }

    // ===== PASSWORD OPERATIONS =====
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var query = $@"
            SELECT username, passw, name, surname, email, phone, client, company,
                   isblocked, createdat, lastlogintime
            FROM {Table("aridbusers")}
            WHERE LOWER(email) = LOWER(@email) AND isblocked = 0
            LIMIT 1";

        var parameters = new[] { new BigQueryParameter("email", BigQueryDbType.String, email) };
        var result = await _client.ExecuteQueryAsync(query, parameters);
        var row = result.FirstOrDefault();

        return row == null ? null : MapToUser(row);
    }

    public async Task UpdateUserPasswordAsync(string username, string client, string newPasswordHash)
    {
        var query = $@"
            UPDATE {Table("aridbusers")}
            SET passw = @passw
            WHERE username = @username AND client = @client";

        var parameters = new[]
        {
            new BigQueryParameter("username", BigQueryDbType.String, username),
            new BigQueryParameter("client", BigQueryDbType.String, client),
            new BigQueryParameter("passw", BigQueryDbType.String, newPasswordHash)
        };

        await _client.ExecuteQueryAsync(query, parameters);
    }

    // ===== HELPER METHODS =====
    private static User MapToUser(BigQueryRow row, string prefix = "")
    {
        var usernameField = string.IsNullOrEmpty(prefix) ? "username" : $"{prefix}username";
        var clientField = string.IsNullOrEmpty(prefix) ? "client" : $"{prefix}client";
        var createdAtField = string.IsNullOrEmpty(prefix) ? "createdat" : $"{prefix}createdat";

        return new User
        {
            Id = 0,
            Username = row[usernameField]?.ToString() ?? "",
            PasswordHash = row["passw"]?.ToString() ?? "",
            FirstName = row["name"]?.ToString() ?? "",
            LastName = row["surname"]?.ToString() ?? "",
            Email = row["email"]?.ToString() ?? "",
            IsActive = row["isblocked"] == null || (long)row["isblocked"] == 0,
            CreatedAt = row[createdAtField] != null ? ((DateTime)row[createdAtField]).ToUniversalTime() : DateTime.UtcNow,
            LastLoginAt = row["lastlogintime"] != null ? ((DateTime)row["lastlogintime"]).ToUniversalTime() : null,
            Client = row[clientField]?.ToString() ?? "00",
            Phone = row["phone"]?.ToString()
        };
    }
}
