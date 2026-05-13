namespace tunav_backend.DTOs;

public class RolePermissionsDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
}

