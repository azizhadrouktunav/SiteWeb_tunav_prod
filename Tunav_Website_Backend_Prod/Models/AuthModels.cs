namespace tunav_backend.Models;

/// <summary>
/// US-A06 — Données envoyées pour la connexion
/// </summary>
public class LoginRequest
{
	public string Email { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
}

/// <summary>
/// US-A06 — Réponse après connexion réussie
/// </summary>
public class LoginResponse
{
	public int UserId { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;
	public string Token { get; set; } = string.Empty;
	public DateTime ExpiresAt { get; set; }

	/// <summary>Codes permission du rôle (JWT + UI backoffice).</summary>
	public List<string> Permissions { get; set; } = new();

}

/// <summary>
/// US-A08 — Données pour réinitialiser le mot de passe
/// </summary>
public class ResetPasswordRequest
{
	public string NewPassword { get; set; } = string.Empty;
}