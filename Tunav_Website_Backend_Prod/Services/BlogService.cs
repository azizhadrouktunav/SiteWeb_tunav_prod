using Microsoft.EntityFrameworkCore;
using tunav_backend.Models;

namespace tunav_backend.Services
{
    // ── DTOs ─────────────────────────────────────────────────────────────────

    public record BlogCategoryDto(
        int Id, string Name, string? Description,
        bool IsActive, int ArticleCount);

    public record BlogArticleDto(
        int Id, string Title, string? Summary, string Content,
        string? CoverImageUrl,
        string? YoutubeUrl,
        string? YoutubeEmbedId,
        string? Sector,
        DateTime? PublishedAt,
        bool IsActive,
        int CategoryId, string? CategoryName,
        int? CreatedById, string? CreatedByName,
        DateTime CreatedAt, DateTime? UpdatedAt);

    // ── Interface ────────────────────────────────────────────────────────────

    public interface IBlogService
    {
        // Catégories
        Task<List<BlogCategoryDto>> GetCategoriesAsync();
        Task<BlogCategoryDto?> GetCategoryAsync(int id);
        Task<BlogCategoryDto> CreateCategoryAsync(string name, string? description);
        Task<BlogCategoryDto?> UpdateCategoryAsync(int id, string name, string? description, bool isActive);
        Task<bool> DeleteCategoryAsync(int id);

        // Articles
        Task<List<BlogArticleDto>> GetArticlesAsync(int? categoryId = null, bool? isActive = null, string? sector = null);
        Task<BlogArticleDto?> GetArticleAsync(int id);
        Task<BlogArticleDto> CreateArticleAsync(
            string title, string? summary, string content,
            string? coverImageUrl, string? youtubeUrl, string? sector,
            DateTime? publishedAt, int categoryId, int? createdById);
        Task<BlogArticleDto?> UpdateArticleAsync(
            int id, string title, string? summary, string content,
            string? coverImageUrl, string? youtubeUrl, string? sector,
            DateTime? publishedAt, int categoryId, bool isActive);
        Task<bool> DeleteArticleAsync(int id);
        Task<BlogArticleDto?> ToggleArticleAsync(int id);
    }

    // ── Implémentation ────────────────────────────────────────────────────────

    public class BlogService : IBlogService
    {
        private readonly AppDbContext _db;
        public BlogService(AppDbContext db) => _db = db;

        // ── CATÉGORIES ──────────────────────────────────────────────────────

        public async Task<List<BlogCategoryDto>> GetCategoriesAsync()
        {
            return await _db.BlogCategories
                .OrderBy(c => c.Name)
                .Select(c => new BlogCategoryDto(
                    c.Id, c.Name, c.Description, c.IsActive,
                    c.Articles.Count(a => a.IsActive)))
                .ToListAsync();
        }

        public async Task<BlogCategoryDto?> GetCategoryAsync(int id)
        {
            var c = await _db.BlogCategories
                .Include(x => x.Articles)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return null;
            return new BlogCategoryDto(c.Id, c.Name, c.Description, c.IsActive,
                c.Articles.Count(a => a.IsActive));
        }

        public async Task<BlogCategoryDto> CreateCategoryAsync(string name, string? description)
        {
            var cat = new BlogCategory { Name = name, Description = description };
            _db.BlogCategories.Add(cat);
            await _db.SaveChangesAsync();
            return new BlogCategoryDto(cat.Id, cat.Name, cat.Description, cat.IsActive, 0);
        }

        public async Task<BlogCategoryDto?> UpdateCategoryAsync(
            int id, string name, string? description, bool isActive)
        {
            var cat = await _db.BlogCategories.FindAsync(id);
            if (cat is null) return null;
            cat.Name = name;
            cat.Description = description;
            cat.IsActive = isActive;
            await _db.SaveChangesAsync();
            var count = await _db.BlogArticles.CountAsync(a => a.CategoryId == id && a.IsActive);
            return new BlogCategoryDto(cat.Id, cat.Name, cat.Description, cat.IsActive, count);
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var cat = await _db.BlogCategories.FindAsync(id);
            if (cat is null) return false;
            _db.BlogCategories.Remove(cat);
            await _db.SaveChangesAsync();
            return true;
        }

        // ── ARTICLES ────────────────────────────────────────────────────────

        private static BlogArticleDto ToDto(BlogArticle a) => new(
            a.Id, a.Title, a.Summary, a.Content, a.CoverImageUrl,
            a.YoutubeUrl, a.YoutubeEmbedId, a.Sector, a.PublishedAt,
            a.IsActive, a.CategoryId, a.CategoryName,
            a.CreatedById, a.CreatedByName,
            a.CreatedAt, a.UpdatedAt);

        private IQueryable<BlogArticle> ArticlesQuery() =>
            _db.BlogArticles
                .Include(a => a.Category)
                .Include(a => a.CreatedBy);

        public async Task<List<BlogArticleDto>> GetArticlesAsync(
            int? categoryId = null, bool? isActive = null, string? sector = null)
        {
            var q = ArticlesQuery();
            if (categoryId.HasValue) q = q.Where(a => a.CategoryId == categoryId);
            if (isActive.HasValue) q = q.Where(a => a.IsActive == isActive);
            if (!string.IsNullOrWhiteSpace(sector)) q = q.Where(a => a.Sector == sector);
            return await q.OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
                          .Select(a => ToDto(a))
                          .ToListAsync();
        }

        public async Task<BlogArticleDto?> GetArticleAsync(int id)
        {
            var a = await ArticlesQuery().FirstOrDefaultAsync(x => x.Id == id);
            return a is null ? null : ToDto(a);
        }

        public async Task<BlogArticleDto> CreateArticleAsync(
            string title, string? summary, string content,
            string? coverImageUrl, string? youtubeUrl, string? sector,
            DateTime? publishedAt, int categoryId, int? createdById)
        {
            var article = new BlogArticle
            {
                Title = title,
                Summary = summary,
                Content = content,
                CoverImageUrl = coverImageUrl,
                YoutubeUrl = youtubeUrl,
                Sector = sector,
                PublishedAt = publishedAt,
                CategoryId = categoryId,
                CreatedById = createdById,
                CreatedAt = DateTime.UtcNow
            };
            _db.BlogArticles.Add(article);
            await _db.SaveChangesAsync();

            var full = await ArticlesQuery().FirstAsync(a => a.Id == article.Id);
            return ToDto(full);
        }

        public async Task<BlogArticleDto?> UpdateArticleAsync(
            int id, string title, string? summary, string content,
            string? coverImageUrl, string? youtubeUrl, string? sector,
            DateTime? publishedAt, int categoryId, bool isActive)
        {
            var article = await ArticlesQuery().FirstOrDefaultAsync(a => a.Id == id);
            if (article is null) return null;

            article.Title = title;
            article.Summary = summary;
            article.Content = content;
            article.CoverImageUrl = coverImageUrl;
            article.YoutubeUrl = youtubeUrl;
            article.Sector = sector;
            article.PublishedAt = publishedAt;
            article.CategoryId = categoryId;
            article.IsActive = isActive;
            article.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            var full = await ArticlesQuery().FirstAsync(a => a.Id == id);
            return ToDto(full);
        }

        public async Task<bool> DeleteArticleAsync(int id)
        {
            var article = await _db.BlogArticles.FindAsync(id);
            if (article is null) return false;
            _db.BlogArticles.Remove(article);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<BlogArticleDto?> ToggleArticleAsync(int id)
        {
            var article = await ArticlesQuery().FirstOrDefaultAsync(a => a.Id == id);
            if (article is null) return null;
            article.IsActive = !article.IsActive;
            article.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return ToDto(article);
        }
    }
}