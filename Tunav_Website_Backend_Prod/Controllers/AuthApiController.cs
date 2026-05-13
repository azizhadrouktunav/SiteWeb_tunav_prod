using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using tunav_backend.Authorization;
using tunav_backend.Models;

namespace tunav_backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthApiController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email et mot de passe sont obligatoires." });

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
            return Unauthorized(new { message = "Email ou mot de passe incorrect." });

        if (!user.IsActive)
            return Unauthorized(new { message = "Votre compte est désactivé. Contactez l'administrateur." });

        var hashedInput = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(request.Password));

        if (user.PasswordHash != hashedInput)
            return Unauthorized(new { message = "Email ou mot de passe incorrect." });

        var permissionCodes = await (
            from rp in _context.RolePermissions
            join p in _context.Permissions on rp.PermissionId equals p.Id
            where rp.RoleId == user.RoleId
            select p.Code).ToListAsync();

        var token = GenerateJwtToken(user, permissionCodes);

        return Ok(new LoginResponse
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            Permissions = permissionCodes
        });
    }

    
    [AllowAnonymous]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Déconnexion réussie. À bientôt !" });
    }

    
    private string GenerateJwtToken(User user, IReadOnlyList<string> permissionCodes)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "tunav_secret_key_2026_backoffice_super_secure_key";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "tunav-backend";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name,               $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role,               user.Role?.Name ?? string.Empty),
            new Claim("userId",                      user.Id.ToString()),
            new Claim("roleName",                    user.Role?.Name ?? string.Empty),
        };

        foreach (var code in permissionCodes)
            claims.Add(new Claim(TunavClaimTypes.Permission, code));

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}