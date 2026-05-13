using Microsoft.EntityFrameworkCore;
using tunav_backend.DTOs;
using tunav_backend.Models;

namespace tunav_backend.Services;

public class PermissionService : IPermissionService
{
    private readonly AppDbContext _context;

    public PermissionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync()
    {
        return await _context.Permissions
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description
            })
            .OrderBy(p => p.Code)
            .ToListAsync();
    }

    public async Task<RolePermissionsDto> GetPermissionsByRoleAsync(int roleId)
    {
        var role = await _context.Roles.FindAsync(roleId);
        if (role == null)
        {
            throw new KeyNotFoundException($"Role with ID {roleId} not found.");
        }

        var permissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.Permission)
            .Select(rp => new PermissionDto
            {
                Id = rp.Permission!.Id,
                Code = rp.Permission.Code,
                Description = rp.Permission.Description
            })
            .OrderBy(p => p.Code)
            .ToListAsync();

        return new RolePermissionsDto
        {
            RoleId = roleId,
            RoleName = role.Name,   // ✅ corrigé: RoleCode → Name
            Permissions = permissions
        };
    }

    public async Task<RolePermissionsDto> UpdatePermissionsForRoleAsync(int roleId, List<int> permissionIds)
    {
        permissionIds ??= new List<int>();

        var role = await _context.Roles.FindAsync(roleId);
        if (role == null)
        {
            throw new KeyNotFoundException($"Role with ID {roleId} not found.");
        }

        var validPermissions = await _context.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        if (validPermissions.Count != permissionIds.Count)
        {
            throw new InvalidOperationException("One or more permission IDs are invalid.");
        }

        var existingMappings = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        _context.RolePermissions.RemoveRange(existingMappings);

        foreach (var permissionId in permissionIds)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }

        await _context.SaveChangesAsync();

        return await GetPermissionsByRoleAsync(roleId);
    }
}
