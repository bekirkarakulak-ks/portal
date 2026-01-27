import api from './api';

// Types
export interface UserListItem {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  client: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  roles: string[];
}

export interface UserDetail {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  client: string;
  phone: string | null;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  roles: RoleResponse[];
  permissions: string[];
}

export interface RoleResponse {
  id: number;
  code: string;
  name: string;
  description: string | null;
  isActive: boolean;
  permissionIds: number[] | null;
}

export interface ModuleResponse {
  id: number;
  code: string;
  name: string;
  description: string | null;
  icon: string | null;
  displayOrder: number;
  isActive: boolean;
  subModules: SubModuleResponse[];
}

export interface SubModuleResponse {
  id: number;
  moduleId: number;
  code: string;
  name: string;
  description: string | null;
  displayOrder: number;
  isActive: boolean;
  permissions: PermissionResponse[];
}

export interface PermissionResponse {
  id: number;
  subModuleId: number;
  levelId: number;
  code: string;
  name: string;
  description: string | null;
  subModuleName: string | null;
  moduleName: string | null;
  levelName: string | null;
}

export interface PermissionLevelResponse {
  id: number;
  level: number;
  code: string;
  name: string;
  description: string | null;
}

export interface OrganizationResponse {
  id: number;
  emailPattern: string;
  departmentCode: string;
  departmentName: string;
  defaultRoleId: number;
  defaultRoleName: string | null;
  priority: number;
  isActive: boolean;
}

export interface AdminDashboard {
  totalUsers: number;
  activeUsers: number;
  totalRoles: number;
  totalModules: number;
  totalPermissions: number;
  organizationRules: number;
  recentUsers: UserListItem[];
}

// API Functions
export const adminService = {
  // Dashboard
  getDashboard: () => api.get<AdminDashboard>('/admin/dashboard'),

  // Users
  getUsers: () => api.get<UserListItem[]>('/admin/users'),
  getUser: (username: string, client: string = '00') =>
    api.get<UserDetail>(`/admin/users/${username}?client=${client}`),
  updateUser: (username: string, client: string, data: { firstName: string; lastName: string; email: string; isActive: boolean }) =>
    api.put(`/admin/users/${username}?client=${client}`, data),
  updateUserRoles: (username: string, roleIds: number[]) =>
    api.put(`/admin/users/${username}/roles`, { roleIds }),
  deleteUser: (username: string, client: string = '00') =>
    api.delete(`/admin/users/${username}?client=${client}`),

  // Roles
  getRoles: () => api.get<RoleResponse[]>('/admin/roles'),
  getRole: (id: number) => api.get<RoleResponse>(`/admin/roles/${id}`),
  createRole: (data: { code: string; name: string; description?: string; permissionIds?: number[] }) =>
    api.post<RoleResponse>('/admin/roles', data),
  updateRole: (id: number, data: { code: string; name: string; description?: string; isActive: boolean; permissionIds?: number[] }) =>
    api.put(`/admin/roles/${id}`, data),
  deleteRole: (id: number) => api.delete(`/admin/roles/${id}`),

  // Modules
  getModules: () => api.get<ModuleResponse[]>('/admin/modules'),
  createModule: (data: { code: string; name: string; description?: string; icon?: string; displayOrder: number }) =>
    api.post<ModuleResponse>('/admin/modules', data),
  updateModule: (id: number, data: { code: string; name: string; description?: string; icon?: string; displayOrder: number; isActive: boolean }) =>
    api.put(`/admin/modules/${id}`, data),
  createSubModule: (data: { moduleId: number; code: string; name: string; description?: string; displayOrder: number }) =>
    api.post<SubModuleResponse>('/admin/submodules', data),
  updateSubModule: (id: number, data: { moduleId: number; code: string; name: string; description?: string; displayOrder: number; isActive: boolean }) =>
    api.put(`/admin/submodules/${id}`, data),

  // Permissions
  getPermissions: () => api.get<PermissionResponse[]>('/admin/permissions'),
  getPermissionLevels: () => api.get<PermissionLevelResponse[]>('/admin/permission-levels'),

  // Organization
  getOrganizations: () => api.get<OrganizationResponse[]>('/admin/organization'),
  createOrganization: (data: { emailPattern: string; departmentCode: string; departmentName: string; defaultRoleId: number; priority: number }) =>
    api.post<OrganizationResponse>('/admin/organization', data),
  updateOrganization: (id: number, data: { emailPattern: string; departmentCode: string; departmentName: string; defaultRoleId: number; priority: number; isActive: boolean }) =>
    api.put(`/admin/organization/${id}`, data),
  deleteOrganization: (id: number) => api.delete(`/admin/organization/${id}`),
};
