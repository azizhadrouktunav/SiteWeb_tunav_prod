namespace tunav_backend.Models;

/// <summary>
/// Données envoyées par l'Admin pour attribuer un rôle à un utilisateur
/// </summary>
public class AssignRoleRequest
{
    public int RoleId { get; set; }
}