using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using tunav_backend.Authorization;
using tunav_backend.Models;
using tunav_backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Autoriser les uploads volumineux (ex. vidéo pack jusqu'à ~40 Mo + marge)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 52_428_800;
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52_428_800;
});


builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();


builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = false;
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<ISolutionService, SolutionService>();
builder.Services.AddScoped<IPackService, PackService>();
builder.Services.AddScoped<ICustomPackRequestService, CustomPackRequestService>();
builder.Services.AddScoped<ITeamMemberService, TeamMemberService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IDemoRequestService, DemoRequestService>();
builder.Services.AddScoped<IPartnerRequestService, PartnerRequestService>();
builder.Services.AddScoped<INotificationService, SmtpNotificationService>();
builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();
builder.Services.AddScoped<INewsletterEmailService, NewsletterEmailService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Tunav Backend API", Version = "v1" });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Entrez votre token JWT : Bearer {token}"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {{
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
            { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
        },
        Array.Empty<string>()
    }});
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? "tunav_secret_key_2026_backoffice_super_secure_key";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "tunav-backend";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep JWT claim types as emitted (e.g. "permission") so policies match TunavClaimTypes.Permission.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var identity = context.Principal?.Identity as ClaimsIdentity;
                if (identity != null)
                {
                    var roleClaim = identity.FindFirst("role")
                                ?? identity.FindFirst("roleName");
                    if (roleClaim != null)
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("ManageUsers", p => p.RequireAssertion(ctx => TunavAuthorization.CanManageUsers(ctx.User)));
    options.AddPolicy("ManageRoles", p => p.RequireAssertion(ctx => TunavAuthorization.CanManageRoles(ctx.User)));
    options.AddPolicy("ManagePermissions", p => p.RequireAssertion(ctx => TunavAuthorization.CanManagePermissionsMatrix(ctx.User)));
    options.AddPolicy("BlogWrite", p => p.RequireAssertion(ctx => TunavAuthorization.CanBlogWrite(ctx.User)));
    options.AddPolicy("EventWrite", p => p.RequireAssertion(ctx => TunavAuthorization.CanEventWrite(ctx.User)));
    options.AddPolicy("CollaborationRead", p => p.RequireAssertion(ctx => TunavAuthorization.CanCollaborationRead(ctx.User)));
    options.AddPolicy("HrWrite", p => p.RequireAssertion(ctx => TunavAuthorization.CanHrWrite(ctx.User)));
    options.AddPolicy("NewsletterWrite", p => p.RequireAssertion(ctx => TunavAuthorization.CanNewsletterWrite(ctx.User)));
    options.AddPolicy("ContactRead", p => p.RequireAssertion(ctx => TunavAuthorization.CanContactRead(ctx.User)));
    options.AddPolicy("SolutionWrite", p => p.RequireAssertion(ctx => TunavAuthorization.CanSolutionWrite(ctx.User)));
    options.AddPolicy("TeamWrite", p => p.RequireAssertion(ctx => TunavAuthorization.CanTeamWrite(ctx.User)));
    // Secteurs d'Activité : Admin + Marketing uniquement
    options.AddPolicy("SectorWrite", p => p.RequireRole("Admin", "Marketing"));
    // ── Portail Partenaire ────────────────────────────────────────────────────
    // Partenaire : soumettre ses propres réclamations et demandes
    options.AddPolicy("PartnerPortalAccess", p => p.RequireRole("Partenaire"));
    // SAV : voir et traiter toutes les réclamations
    options.AddPolicy("SavAccess", p => p.RequireRole("Admin", "SAV"));
    // Commercial : voir et traiter toutes les demandes partenaires
    options.AddPolicy("CommercialDemandsAccess", p => p.RequireRole("Admin", "Commercial"));
});

// CORS : front Angular (localhost) + domaines prod (config). www et apex sont des origines distinctes.
var localFrontendOrigins = new[]
{
    "http://localhost:4200",
    "https://localhost:4200",
    "http://localhost:5057",
    "https://localhost:5057",
    "http://localhost:7000",
    "https://localhost:7000",
    "http://localhost:7110",
    "https://localhost:7110"
};
// Toujours autoriser le site public même si Cors:AllowedOrigins est absent ou écrasé sur le serveur.
var productionPublicOrigins = new[]
{
    "https://tunav.com",
    "https://www.tunav.com",
    "http://tunav.com",
    "http://www.tunav.com"
};
var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
var corsAllowedOrigins = localFrontendOrigins
    .Concat(productionPublicOrigins)
    .Concat(configuredCorsOrigins)
    .Select(o => (o ?? "").Trim().TrimEnd('/'))
    .Where(o => o.Length > 0)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ── Seed ─────────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        if (db.Database.GetPendingMigrations().Any())
            db.Database.Migrate();
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(
            "Auto-migration skipped: {Message}. Run 'dotnet ef database update' manually if needed.",
            ex.Message);
    }

    var rolesParDefaut = new[]
    {
        new Role { Name = "Admin",               Description = "Acces complet au backoffice",          IsActive = true },
        new Role { Name = "Marketing",           Description = "Gestion du contenu du site",           IsActive = true },
        new Role { Name = "Ressources Humaines", Description = "Gestion du recrutement et des stages", IsActive = true },
        new Role { Name = "Commercial",          Description = "Suivi des opportunites commerciales",  IsActive = true },
        new Role { Name = "SAV",                 Description = "Traitement des reclamations",          IsActive = true },
        new Role { Name = "Partenaire",          Description = "Acces limite au backoffice",           IsActive = true },
    };
    foreach (var role in rolesParDefaut)
        if (!db.Roles.Any(r => r.Name == role.Name)) db.Roles.Add(role);
    db.SaveChanges();

    if (!db.Users.Any(u => u.Email == "admin@tunav.tn"))
    {
        var adminRole = db.Roles.First(r => r.Name == "Admin");
        db.Users.Add(new User
        {
            FirstName = "Super",
            LastName = "Admin",
            Email = "admin@tunav.tn",
            PasswordHash = Convert.ToBase64String(Encoding.UTF8.GetBytes("admin123")),
            RoleId = adminRole.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    var allPermissions = new[]
    {
        new { Code = "blog.article.create",        Description = "US-BL05 – Ajouter un article" },
        new { Code = "blog.article.edit",          Description = "US-BL06 – Modifier un article" },
        new { Code = "blog.article.delete",        Description = "US-BL07 – Supprimer un article" },
        new { Code = "blog.article.categorize",    Description = "US-BL08 – Catégoriser un article" },
        new { Code = "blog.article.toggle",        Description = "US-BL09 – Activer/Désactiver un article" },
        new { Code = "blog.category.create",       Description = "US-BL10 – Ajouter une catégorie" },
        new { Code = "blog.category.edit",         Description = "US-BL11 – Modifier une catégorie" },
        new { Code = "blog.category.delete",       Description = "US-BL12 – Supprimer une catégorie" },
        new { Code = "event.create",               Description = "US-EV01 – Créer un événement" },
        new { Code = "event.edit",                 Description = "US-EV02 – Modifier un événement" },
        new { Code = "event.delete",               Description = "US-EV03 – Supprimer un événement" },
        new { Code = "event.publish",              Description = "US-EV04 – Publier un événement" },
        new { Code = "event.archive",              Description = "US-EV05 – Archiver un événement" },
        new { Code = "event.collaboration.view",   Description = "US-EV06 – Voir les collaborations" },
        new { Code = "event.collaboration.manage", Description = "US-EV07 – Gérer les collaborations" },
        new { Code = "event.collaboration.export", Description = "US-EV08 – Exporter les demandes" },
        new { Code = "event.collaboration.note",   Description = "US-EV09 – Note interne" },
        new { Code = "event.collaboration.status", Description = "US-EV10 – Changer statut" },
        new { Code = "job.offer.create",           Description = "US-EMP07 – Ajouter une offre" },
        new { Code = "job.offer.edit",             Description = "US-EMP08 – Modifier une offre" },
        new { Code = "job.offer.delete",           Description = "US-EMP09 – Supprimer une offre" },
        new { Code = "job.offer.toggle",           Description = "US-EMP10 – Activer/Désactiver une offre" },
        new { Code = "job.application.view",       Description = "US-EMP11 – Voir les candidatures" },
        new { Code = "job.application.status",     Description = "US-EMP13 – Statut candidature" },
        new { Code = "contact.request.view",       Description = "Voir les demandes de contact" },
        new { Code = "contact.request.status",     Description = "Changer le statut d'une demande de contact" },
        new { Code = "contact.demo.view",          Description = "Voir les demandes de rendez-vous" },
        new { Code = "contact.demo.status",        Description = "Changer le statut d'un rendez-vous" },
        new { Code = "system.users.manage",        Description = "Gérer les comptes utilisateurs" },
        new { Code = "system.roles.manage",        Description = "Gérer les rôles" },
        new { Code = "system.permissions.manage",  Description = "Gérer les matrices de permissions" },
        new { Code = "solution.manage",            Description = "Gérer les solutions (contenu site)" },
        new { Code = "team.manage",                Description = "Gérer les membres de l'équipe" },
        new { Code = "newsletter.manage",         Description = "Gérer les newsletters et abonnés" },
        new { Code = "training.manage",            Description = "Gérer le contenu pôle formation" },
    };
    foreach (var p in allPermissions)
        if (!db.Permissions.Any(x => x.Code == p.Code))
            db.Permissions.Add(new Permission { Code = p.Code, Description = p.Description });
    db.SaveChanges();

    var allPermIds = db.Permissions.Select(p => p.Id).ToList();
    var adminRoleId = db.Roles.First(r => r.Name == "Admin").Id;
    var marketingRoleId = db.Roles.First(r => r.Name == "Marketing").Id;
    var rhRoleId = db.Roles.First(r => r.Name == "Ressources Humaines").Id;
    var commercialRoleId = db.Roles.First(r => r.Name == "Commercial").Id;

    var contactPermCodes = new[] { "contact.request.view", "contact.request.status", "contact.demo.view", "contact.demo.status" };
    var contactPermIds = db.Permissions.Where(p => contactPermCodes.Contains(p.Code)).Select(p => p.Id).ToList();

    // Pas de StringComparison dans LINQ→SQL (Npgsql ne traduit pas StartsWith(..., OrdinalIgnoreCase)).
    var systemPermIds = db.Permissions
        .Where(p => p.Code.StartsWith("system."))
        .Select(p => p.Id)
        .ToHashSet();

    var jobPermIds = db.Permissions
        .Where(p => p.Code.StartsWith("job."))
        .Select(p => p.Id)
        .ToHashSet();

    var contactOnlyPermIds = contactPermIds.ToHashSet();

    void EnsureRolePermission(int roleId, int permissionId)
    {
        if (!db.RolePermissions.Any(rp => rp.RoleId == roleId && rp.PermissionId == permissionId))
            db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
    }

    foreach (var permId in allPermIds)
    {
        EnsureRolePermission(adminRoleId, permId);
        if (!systemPermIds.Contains(permId))
        {
            // Marketing : contenu / comm' — pas RH, pas commercial pur, pas administration système
            if (!jobPermIds.Contains(permId) && !contactOnlyPermIds.Contains(permId))
                EnsureRolePermission(marketingRoleId, permId);
            EnsureRolePermission(rhRoleId, permId);
        }
    }
    foreach (var permId in contactPermIds)
    {
        if (!db.RolePermissions.Any(rp => rp.RoleId == commercialRoleId && rp.PermissionId == permId))
            db.RolePermissions.Add(new RolePermission { RoleId = commercialRoleId, PermissionId = permId });
    }

    // Nettoyer d'éventuelles liaisons incohérentes (anciennes bases où Marketing avait RH / contact / system)
    var marketingJunk = db.RolePermissions.Where(rp =>
        rp.RoleId == marketingRoleId
        && (systemPermIds.Contains(rp.PermissionId)
            || jobPermIds.Contains(rp.PermissionId)
            || contactOnlyPermIds.Contains(rp.PermissionId)));
    db.RolePermissions.RemoveRange(marketingJunk);

    db.SaveChanges();

    // ── Seed Secteurs d'Activité par défaut ───────────────────────────────────
    if (!db.IndustrySectors.Any())
    {
        db.IndustrySectors.AddRange(new[]
        {
            new IndustrySector { Title = "SANTÉ & MÉDICAL",         Description = "Solutions sécurisées pour la gestion des données patients, l'optimisation des flux et la conformité réglementaire.",          ImageUrl = "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=500&h=300&fit=crop", DisplayOrder = 1, IsActive = true },
            new IndustrySector { Title = "INDUSTRIE & MANUFACTURE",  Description = "Automatisation de la chaîne de production, maintenance prédictive et suivi logistique en temps réel.",                       ImageUrl = "https://images.unsplash.com/photo-1581092160562-40aa08e78837?w=500&h=300&fit=crop", DisplayOrder = 2, IsActive = true },
            new IndustrySector { Title = "SERVICES FINANCIERS",      Description = "Plateformes robustes pour les transactions sécurisées, l'analyse de risques et l'expérience client digitale.",                ImageUrl = "https://images.unsplash.com/photo-1454165804606-c3d57bc86b40?w=500&h=300&fit=crop", DisplayOrder = 3, IsActive = true },
            new IndustrySector { Title = "TRANSPORT & LOGISTIQUE",   Description = "Optimisation des routes, suivi de flotte en temps réel et gestion efficace de la supply chain.",                              ImageUrl = "https://images.unsplash.com/photo-1601584115197-04ecc0da31d7?w=500&h=300&fit=crop", DisplayOrder = 4, IsActive = true },
            new IndustrySector { Title = "ADMINISTRATION PUBLIQUE",  Description = "Digitalisation des services publics et gestion intelligente des actifs de l'État.",                                            ImageUrl = "https://images.unsplash.com/photo-1568992687947-868a62a9f521?w=500&h=300&fit=crop", DisplayOrder = 5, IsActive = true },
            new IndustrySector { Title = "ÉNERGIE & UTILITIES",      Description = "Surveillance et contrôle des infrastructures énergétiques avec des solutions IoT avancées.",                                   ImageUrl = "https://images.unsplash.com/photo-1473341304170-971dccb5ac1e?w=500&h=300&fit=crop", DisplayOrder = 6, IsActive = true },
            new IndustrySector { Title = "AGRICULTURE",              Description = "Agriculture de précision grâce aux capteurs IoT et au suivi en temps réel des équipements agricoles.",                         ImageUrl = "https://images.unsplash.com/photo-1625246333195-78d9c38ad449?w=500&h=300&fit=crop", DisplayOrder = 7, IsActive = true },
        });
        db.SaveChanges();
    }

    startupLogger.LogInformation("Startup seed completed.");
}

// ── Pipeline ──────────────────────────────────────────────────────────────────
// Optional: when the app is not at the domain root (e.g. https://tunav.com/backend/…).
// Leave empty when hosted as an IIS virtual app (PathBase is usually set automatically).
var configuredPathBase = builder.Configuration["PathBase"]?.Trim()
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE")?.Trim()
    ?? "";
if (!string.IsNullOrEmpty(configuredPathBase))
{
    if (!configuredPathBase.StartsWith('/'))
        configuredPathBase = "/" + configuredPathBase;
    configuredPathBase = configuredPathBase.TrimEnd('/');
    app.UsePathBase(configuredPathBase);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Redirection HTTPS uniquement si HTTPS est effectivement configuré
var httpsConfigured = app.Urls.Any(url => url.StartsWith("https://"))
    || !string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_HTTPS_PORT"])
    || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT"));
// In Development, keep HTTP working for local frontends (avoids CORS-blocked 307 redirects).
if (!app.Environment.IsDevelopment() && httpsConfigured)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "Uploads");
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseRouting();
app.UseCors("FrontendPolicy");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/", () => Results.Redirect("/auth/login"));

app.Run();