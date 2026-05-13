namespace tunav_backend.Models;

/// <summary>
/// Représente une carte "Secteur d'activité" affichée dans le carousel
/// de la section "Solutions Adaptées à Votre Industrie" sur la page d'accueil.
/// </summary>
public class IndustrySector
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>URL de l'image de couverture (Unsplash ou /uploads/...)</summary>
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}