using tunav_backend.DTOs;
using tunav_backend.Models;

namespace tunav_backend.Services;

/// <summary>
/// Interface for solution management service
/// Allows marketing staff to manage solutions
/// </summary>
public interface ISolutionService
{
    Task<IReadOnlyList<string>> GetAvailableSectorsAsync();

    /// <summary>
    /// Get all solutions (optionally filtered)
    /// </summary>
    Task<IEnumerable<SolutionDto>> GetAllSolutionsAsync(
        bool? isActiveOnly = null,
        bool? isActive = null,
        string? type = null,
        string? sector = null,
        string? search = null);

    /// <summary>
    /// Get a specific solution by ID
    /// </summary>
    Task<SolutionDto?> GetSolutionByIdAsync(int id);

    /// <summary>
    /// Get solutions filtered by type (General or Sectorial)
    /// </summary>
    Task<IEnumerable<SolutionDto>> GetSolutionsByTypeAsync(string type);

    /// <summary>
    /// Create a new solution
    /// </summary>
    /// <param name="dto">Create solution data</param>
    /// <param name="userId">ID of the user creating the solution</param>
    /// <returns>The created solution</returns>
    Task<SolutionDto> CreateSolutionAsync(CreateSolutionDto dto, int userId);

    /// <summary>
    /// Update an existing solution
    /// </summary>
    /// <param name="id">Solution ID</param>
    /// <param name="dto">Update solution data</param>
    /// <returns>The updated solution</returns>
    Task<SolutionDto> UpdateSolutionAsync(int id, UpdateSolutionDto dto);

    /// <summary>
    /// Delete a solution permanently
    /// </summary>
    Task<bool> DeleteSolutionAsync(int id);
}

