using tunav_backend.DTOs;

namespace tunav_backend.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface ITeamMemberService
{
    /// <summary>US-PE05 / US-PE08 — Liste membres (public: actifs seulement)</summary>
    Task<IEnumerable<TeamMemberDto>> GetAllAsync(bool? isActiveOnly = null);

    /// <summary>Détail d'un membre</summary>
    Task<TeamMemberDto?> GetByIdAsync(int id);

    /// <summary>US-PE08 — Ajout d'un membre</summary>
    Task<TeamMemberDto> CreateAsync(CreateTeamMemberDto dto, int userId);

    /// <summary>US-PE09 — Modification d'un membre</summary>
    Task<TeamMemberDto> UpdateAsync(int id, UpdateTeamMemberDto dto);

    /// <summary>US-PE11 — Activation / Désactivation</summary>
    Task<TeamMemberDto> ToggleActiveAsync(int id);

    /// <summary>US-PE10 — Suppression d'un membre</summary>
    Task<bool> DeleteAsync(int id);
}