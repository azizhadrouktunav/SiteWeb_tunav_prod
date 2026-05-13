using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tunav_backend.Services;

namespace tunav_backend.Controllers
{
   

    [ApiController]
    [Route("api/blog-categories")]
    public class BlogCategoriesController : ControllerBase
    {
        private readonly IBlogService _blog;
        public BlogCategoriesController(IBlogService blog) => _blog = blog;

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> GetAll() =>
            Ok(await _blog.GetCategoriesAsync());

        [HttpGet("{id:int}"), AllowAnonymous]
        public async Task<IActionResult> GetOne(int id)
        {
            var cat = await _blog.GetCategoryAsync(id);
            return cat is null ? NotFound(new { message = "Catégorie introuvable." }) : Ok(cat);
        }

        [HttpPost, Authorize(Policy = "BlogWrite")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { message = "Le nom est obligatoire." });

            var cat = await _blog.CreateCategoryAsync(req.Name.Trim(), req.Description?.Trim());
            return CreatedAtAction(nameof(GetOne), new { id = cat.Id }, cat);
        }

        [HttpPut("{id:int}"), Authorize(Policy = "BlogWrite")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { message = "Le nom est obligatoire." });

            var cat = await _blog.UpdateCategoryAsync(id, req.Name.Trim(), req.Description?.Trim(), req.IsActive);
            return cat is null ? NotFound(new { message = "Catégorie introuvable." }) : Ok(cat);
        }

        [HttpDelete("{id:int}"), Authorize(Policy = "BlogWrite")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _blog.DeleteCategoryAsync(id);
            return ok ? NoContent() : NotFound(new { message = "Catégorie introuvable." });
        }
    }

    
    [ApiController]
    [Route("api/blog-articles")]
    public class BlogArticlesController : ControllerBase
    {
        private readonly IBlogService _blog;
        public BlogArticlesController(IBlogService blog) => _blog = blog;

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? categoryId,
            [FromQuery] bool? isActive,
            [FromQuery] string? sector)
        {
            return Ok(await _blog.GetArticlesAsync(categoryId, isActive, sector));
        }

        [HttpGet("{id:int}"), AllowAnonymous]
        public async Task<IActionResult> GetOne(int id)
        {
            var a = await _blog.GetArticleAsync(id);
            return a is null ? NotFound(new { message = "Article introuvable." }) : Ok(a);
        }

        [HttpPost, Authorize(Policy = "BlogWrite")]
        public async Task<IActionResult> Create(
            [FromQuery] int? userId,
            [FromBody] CreateArticleRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest(new { message = "Le titre est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.Content))
                return BadRequest(new { message = "Le contenu est obligatoire." });
            if (req.CategoryId <= 0)
                return BadRequest(new { message = "La catégorie est obligatoire." });

            var article = await _blog.CreateArticleAsync(
                req.Title.Trim(), req.Summary?.Trim(), req.Content.Trim(),
                req.CoverImageUrl?.Trim(), req.YoutubeUrl?.Trim(),
                req.Sector?.Trim(), req.PublishedAt,
                req.CategoryId, userId);

            return CreatedAtAction(nameof(GetOne), new { id = article.Id }, article);
        }

        [HttpPut("{id:int}"), Authorize(Policy = "BlogWrite")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateArticleRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest(new { message = "Le titre est obligatoire." });
            if (string.IsNullOrWhiteSpace(req.Content))
                return BadRequest(new { message = "Le contenu est obligatoire." });
            if (req.CategoryId <= 0)
                return BadRequest(new { message = "La catégorie est obligatoire." });

            var article = await _blog.UpdateArticleAsync(
                id, req.Title.Trim(), req.Summary?.Trim(), req.Content.Trim(),
                req.CoverImageUrl?.Trim(), req.YoutubeUrl?.Trim(),
                req.Sector?.Trim(), req.PublishedAt,
                req.CategoryId, req.IsActive);

            return article is null ? NotFound(new { message = "Article introuvable." }) : Ok(article);
        }

        [HttpPatch("{id:int}/toggle"), Authorize(Policy = "BlogWrite")]
        public async Task<IActionResult> Toggle(int id)
        {
            var article = await _blog.ToggleArticleAsync(id);
            return article is null ? NotFound(new { message = "Article introuvable." }) : Ok(article);
        }

        [HttpDelete("{id:int}"), Authorize(Policy = "BlogWrite")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _blog.DeleteArticleAsync(id);
            return ok ? NoContent() : NotFound(new { message = "Article introuvable." });
        }
    }

    public record CreateCategoryRequest(string Name, string? Description);
    public record UpdateCategoryRequest(string Name, string? Description, bool IsActive);

    public record CreateArticleRequest(
        string Title,
        string? Summary,
        string Content,
        string? CoverImageUrl,
        string? YoutubeUrl,
        string? Sector,
        DateTime? PublishedAt,
        int CategoryId);

    public record UpdateArticleRequest(
        string Title,
        string? Summary,
        string Content,
        string? CoverImageUrl,
        string? YoutubeUrl,
        string? Sector,
        DateTime? PublishedAt,
        int CategoryId,
        bool IsActive);
}