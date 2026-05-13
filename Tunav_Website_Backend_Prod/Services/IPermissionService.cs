using tunav_backend.DTOs;

namespace tunav_backend.Services;

public interface IPermissionService
{
    Task<List<PermissionDto>> GetAllPermissionsAsync();
    Task<RolePermissionsDto> GetPermissionsByRoleAsync(int roleId);
    Task<RolePermissionsDto> UpdatePermissionsForRoleAsync(int roleId, List<int> permissionIds);
}

