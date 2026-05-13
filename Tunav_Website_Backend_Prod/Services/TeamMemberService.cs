using Microsoft.EntityFrameworkCore;
using tunav_backend.DTOs;
using tunav_backend.Models;

namespace tunav_backend.Services;

public class TeamMemberService : ITeamMemberService
{
    private readonly AppDbContext _context;

    public TeamMemberService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TeamMemberDto>> GetAllAsync(bool? isActiveOnly = null)
    {
        var query = _context.TeamMembers.Include(m => m.CreatedByUser).AsQueryable();

        if (isActiveOnly.HasValue && isActiveOnly.Value)
            query = query.Where(m => m.IsActive);

        var members = await query
            .OrderBy(m => m.DisplayOrder)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();

        return members.Select(MapToDto);
    }

    public async Task<TeamMemberDto?> GetByIdAsync(int id)
    {
        var member = await _context.TeamMembers
            .Include(m => m.CreatedByUser)
            .FirstOrDefaultAsync(m => m.Id == id);

        return member != null ? MapToDto(member) : null;
    }

    public async Task<TeamMemberDto> CreateAsync(CreateTeamMemberDto dto, int userId)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            throw new InvalidOperationException($"Utilisateur avec l'ID {userId} introuvable.");

        if (string.IsNullOrWhiteSpace(dto.FirstName))
            throw new InvalidOperationException("Le prénom est obligatoire.");
        if (string.IsNullOrWhiteSpace(dto.LastName))
            throw new InvalidOperationException("Le nom est obligatoire.");
        if (string.IsNullOrWhiteSpace(dto.Position))
            throw new InvalidOperationException("Le poste est obligatoire.");

        var member = new TeamMember
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Position = dto.Position.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            PhotoUrl = dto.PhotoUrl?.Trim(),
            LinkedInUrl = dto.LinkedInUrl?.Trim(),
            Email = dto.Email?.Trim(),
            DisplayOrder = dto.DisplayOrder,
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.TeamMembers.Add(member);
        await _context.SaveChangesAsync();
        await _context.Entry(member).Reference(m => m.CreatedByUser).LoadAsync();
        return MapToDto(member);
    }

    public async Task<TeamMemberDto> UpdateAsync(int id, UpdateTeamMemberDto dto)
    {
        var member = await _context.TeamMembers
            .Include(m => m.CreatedByUser)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (member == null)
            throw new InvalidOperationException($"Membre avec l'ID {id} introuvable.");

        if (!string.IsNullOrWhiteSpace(dto.FirstName)) member.FirstName = dto.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.LastName)) member.LastName = dto.LastName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Position)) member.Position = dto.Position.Trim();
        if (dto.Description != null) member.Description = dto.Description.Trim();
        if (dto.PhotoUrl != null) member.PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim();
        if (dto.LinkedInUrl != null) member.LinkedInUrl = string.IsNullOrWhiteSpace(dto.LinkedInUrl) ? null : dto.LinkedInUrl.Trim();
        if (dto.Email != null) member.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        if (dto.DisplayOrder.HasValue) member.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.IsActive.HasValue) member.IsActive = dto.IsActive.Value;

        member.UpdatedAt = DateTime.UtcNow;
        _context.TeamMembers.Update(member);
        await _context.SaveChangesAsync();
        return MapToDto(member);
    }

    public async Task<TeamMemberDto> ToggleActiveAsync(int id)
    {
        var member = await _context.TeamMembers
            .Include(m => m.CreatedByUser)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (member == null)
            throw new InvalidOperationException($"Membre avec l'ID {id} introuvable.");

        member.IsActive = !member.IsActive;
        member.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapToDto(member);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var member = await _context.TeamMembers.FirstOrDefaultAsync(m => m.Id == id);
        if (member == null)
            throw new InvalidOperationException($"Membre avec l'ID {id} introuvable.");

        _context.TeamMembers.Remove(member);
        await _context.SaveChangesAsync();
        return true;
    }

    private static TeamMemberDto MapToDto(TeamMember m) => new()
    {
        Id = m.Id,
        FirstName = m.FirstName,
        LastName = m.LastName,
        Position = m.Position,
        Description = m.Description,
        PhotoUrl = m.PhotoUrl,
        LinkedInUrl = m.LinkedInUrl,
        Email = m.Email,
        DisplayOrder = m.DisplayOrder,
        IsActive = m.IsActive,
        CreatedByName = m.CreatedByUser != null
            ? $"{m.CreatedByUser.FirstName} {m.CreatedByUser.LastName}".Trim()
            : "—",
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };
}
