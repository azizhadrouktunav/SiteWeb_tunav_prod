using Microsoft.EntityFrameworkCore;

namespace tunav_backend.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Solution> Solutions => Set<Solution>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogArticle> BlogArticles => Set<BlogArticle>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<CollaborationRequest> CollaborationRequests => Set<CollaborationRequest>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<DemoRequest> DemoRequests => Set<DemoRequest>();
    public DbSet<Pack> Packs => Set<Pack>();
    public DbSet<CustomPackRequest> CustomPackRequests => Set<CustomPackRequest>();
    public DbSet<PartnerRequest> PartnerRequests => Set<PartnerRequest>();
    public DbSet<TrainingPartner> TrainingPartners => Set<TrainingPartner>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();

    
    public DbSet<Newsletter> Newsletters => Set<Newsletter>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();

    
    public DbSet<JobOffer> JobOffers => Set<JobOffer>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    
    public DbSet<ContactRequest> ContactRequests => Set<ContactRequest>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Partner> Partners => Set<Partner>();

    
    public DbSet<IndustrySector> IndustrySectors => Set<IndustrySector>();

    // ── Portail Partenaire : Réclamations & Demandes ───────────────
    public DbSet<PartnerClaim> PartnerClaims => Set<PartnerClaim>();
    public DbSet<PartnerDemand> PartnerDemands => Set<PartnerDemand>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Role ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired(false);
        });

        // ── User ────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired(false);
            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Permission ──────────────────────────────────────────────────────
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // ── RolePermission ──────────────────────────────────────────────────
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Solution ────────────────────────────────────────────────────────
        modelBuilder.Entity<Solution>(entity =>
        {
            entity.ToTable("solutions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Type)
                .IsRequired();

            entity.Property(e => e.SectorName)
                .HasMaxLength(255);

            entity.Property(e => e.PackIconKey)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("map-pin");

            entity.Property(e => e.PackThemeKey)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("blue-cyan");

            entity.Property(e => e.CoverImageUrl)
                .HasMaxLength(500);

            entity.Property(e => e.YoutubeUrl)
                .HasMaxLength(500);

            entity.Property(e => e.SectorsJson)
                .HasMaxLength(4000);

            entity.Property(e => e.TopClientsJson)
                .HasMaxLength(4000);

            entity.Property(e => e.FunctionalitiesJson)
                .HasMaxLength(8000);

            entity.Property(e => e.AdvantagesJson)
                .HasMaxLength(8000);

            entity.Property(e => e.UseCasesJson)
                .HasMaxLength(12000);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BaseSolution)
                .WithMany(e => e.DerivedSolutions)
                .HasForeignKey(e => e.BaseSolutionId)
                // SQL Server limitation: avoid cascade path on self-referencing FK.
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Packs)
                .WithOne(p => p.Solution)
                .HasForeignKey(p => p.SolutionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.BaseSolutionId);
        });

        // ── TeamMember ──────────────────────────────────────────────────────
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.ToTable("team_members");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Position).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(1000).HasDefaultValue(string.Empty);
            entity.Property(e => e.PhotoUrl).HasMaxLength(500);
            entity.Property(e => e.LinkedInUrl).HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.DisplayOrder);
        });

        // ── BlogCategory ────────────────────────────────────────────────────
        modelBuilder.Entity<BlogCategory>(entity =>
        {
            entity.ToTable("blog_categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── BlogArticle ─────────────────────────────────────────────────────
        modelBuilder.Entity<BlogArticle>(entity =>
        {
            entity.ToTable("blog_articles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Summary).HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CoverImageUrl).HasMaxLength(500);
            entity.Property(e => e.YoutubeUrl).HasMaxLength(500);
            entity.Property(e => e.Sector).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired(false);
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Articles)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Sector);
            entity.HasIndex(e => e.PublishedAt);
        });

        // ── Event ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Type).IsRequired().HasConversion<string>();
            entity.Property(e => e.Status).IsRequired().HasConversion<string>()
                .HasDefaultValue(EventStatus.Draft);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(300);
            entity.Property(e => e.OnlineLink).HasMaxLength(500);
            entity.Property(e => e.CoverImageUrl).HasMaxLength(500);
            entity.Property(e => e.YoutubeUrl).HasMaxLength(500);
            entity.Property(e => e.ExternalUrl).HasMaxLength(500);
            entity.Property(e => e.IsArchived).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartDate);
            entity.HasIndex(e => e.Type);
        });

        // ── CollaborationRequest ─────────────────────────────────────────────
        modelBuilder.Entity<CollaborationRequest>(entity =>
        {
            entity.ToTable("collaboration_requests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Organization).IsRequired().HasMaxLength(150);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.AttachmentNames).HasMaxLength(500);
            entity.Property(e => e.CollaborationType)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(CollaborationType.Collaboration);
            entity.HasIndex(e => e.CollaborationType);
            entity.Property(e => e.IsHomologueMalek).IsRequired(false);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(CollaborationStatus.Nouvelle);
            entity.Property(e => e.InternalNote).HasMaxLength(500);
            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.EventId).IsRequired(false);
            entity.HasOne(e => e.Event)
                .WithMany(ev => ev.CollaborationRequests)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            entity.HasIndex(e => e.Status);
        });

        // ── EventRegistration ────────────────────────────────────────────────
        modelBuilder.Entity<EventRegistration>(entity =>
        {
            entity.ToTable("event_registrations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Organization).HasMaxLength(100);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(RegistrationStatus.Nouvelle);
            entity.Property(e => e.InternalNote).HasMaxLength(500);
            entity.Property(e => e.RegisteredAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.Event)
                .WithMany(e => e.Registrations)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.EventId);
            entity.HasIndex(e => e.Status);
        });

        // ── TrainingPartner ──────────────────────────────────────────────────
        modelBuilder.Entity<PartnerRequest>(entity =>
        {
            entity.ToTable("partner_requests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PartnerType)
                .IsRequired()
                .HasConversion<string>();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Company).HasMaxLength(150);
            entity.Property(e => e.City).IsRequired().HasMaxLength(150);
            entity.Property(e => e.PersonType)
                .IsRequired()
                .HasConversion<string>();
            entity.Property(e => e.SelectedSolutionsJson)
                .IsRequired()
                .HasMaxLength(4000);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(PartnerRequestStatus.Nouvelle);
            entity.Property(e => e.InternalNote).HasMaxLength(500);
            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired(false);
            entity.HasIndex(e => e.PartnerType);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<DemoRequest>(entity =>
        {
            entity.ToTable("demo_requests");
            entity.HasKey(e => e.Id);
            // demo_requests exists with legacy PascalCase columns, while newer pack
            // fields were added later in snake_case. Keep EF aligned with the live
            // table until we ship an explicit rename migration.
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.SolutionId).HasColumnName("SolutionId").IsRequired();
            entity.Property(e => e.PackId).HasColumnName("pack_id").IsRequired(false);
            entity.Property(e => e.SolutionTitle).HasColumnName("SolutionTitle").IsRequired().HasMaxLength(255);
            entity.Property(e => e.PackName).HasColumnName("pack_name").HasMaxLength(255);
            entity.Property(e => e.FirstName).HasColumnName("FirstName").IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).HasColumnName("LastName").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasColumnName("Email").IsRequired().HasMaxLength(200);
            entity.Property(e => e.HasWhatsapp).HasColumnName("HasWhatsapp").IsRequired();
            entity.Property(e => e.WhatsappNumber).HasColumnName("WhatsappNumber").HasMaxLength(30);
            entity.Property(e => e.EntryPoint)
                .HasColumnName("EntryPoint")
                .IsRequired()
                .HasConversion<string>();
            entity.Property(e => e.Status)
                .HasColumnName("Status")
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(DemoRequestStatus.Nouvelle);
            entity.Property(e => e.InternalNote).HasColumnName("InternalNote").HasMaxLength(500);
            entity.Property(e => e.SubmittedAt).HasColumnName("SubmittedAt").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").IsRequired(false);
            entity.HasIndex(e => e.SolutionId);
            entity.HasIndex(e => e.PackId);
            entity.HasIndex(e => e.EntryPoint);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<Pack>(entity =>
        {
            entity.ToTable("packs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(180);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1200);
            entity.Property(e => e.FeaturesJson).IsRequired().HasMaxLength(12000);
            entity.Property(e => e.ThemeKey).IsRequired().HasMaxLength(50);
            entity.Property(e => e.VideoUrl).HasMaxLength(500);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.IsPopular).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired(false);
            entity.HasIndex(e => e.SolutionId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.SolutionId, e.DisplayOrder });
        });

        modelBuilder.Entity<CustomPackRequest>(entity =>
        {
            entity.ToTable("custom_pack_requests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SolutionId).HasColumnName("solution_id").IsRequired();
            entity.Property(e => e.SolutionTitle).HasColumnName("solution_title").IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContactName).HasColumnName("contact_name").IsRequired().HasMaxLength(160);
            entity.Property(e => e.Company).HasColumnName("company").IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasColumnName("phone").IsRequired().HasMaxLength(30);
            entity.Property(e => e.Message).HasColumnName("message").HasMaxLength(2000);
            entity.Property(e => e.SelectedFeaturesJson).HasColumnName("selected_features_json").IsRequired().HasMaxLength(12000);
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(CustomPackRequestStatus.Nouvelle);
            entity.Property(e => e.InternalNote).HasColumnName("internal_note").HasMaxLength(500);
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired(false);
            entity.HasIndex(e => e.SolutionId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SubmittedAt);
        });

        modelBuilder.Entity<TrainingPartner>(entity =>
        {
            entity.ToTable("training_partners");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Domain).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Icon).HasMaxLength(10);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.DisplayOrder);
        });

        // ── Testimonial ──────────────────────────────────────────────────────
        modelBuilder.Entity<Testimonial>(entity =>
        {
            entity.ToTable("testimonials");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AuthorName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.AuthorRole).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Company).HasMaxLength(150);
            entity.Property(e => e.Avatar).HasMaxLength(10);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.IsActive);
        });

        // ── Newsletter ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Newsletter>(entity =>
        {
            entity.ToTable("newsletters");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Summary).HasMaxLength(500);
            entity.Property(e => e.TableOfContents).HasMaxLength(2000);
            entity.Property(e => e.CoverImageUrl).HasMaxLength(500);
            entity.Property(e => e.PdfUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.PublishedAt);
        });

        // ── JobOffer ──────────────────────────────────────────────────────────
        modelBuilder.Entity<JobOffer>(entity =>
        {
            entity.ToTable("job_offers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(3000);
            entity.Property(e => e.Requirements).HasMaxLength(2000);
            entity.Property(e => e.ContractType).IsRequired().HasConversion<string>();
            entity.Property(e => e.AcademicLevel).IsRequired().HasConversion<string>();
            entity.Property(e => e.PostType).IsRequired().HasConversion<string>();
            entity.Property(e => e.Status).IsRequired().HasConversion<string>()
                .HasDefaultValue(JobStatus.Active);
            entity.Property(e => e.Location).HasMaxLength(150);
            entity.Property(e => e.Duration).HasMaxLength(100);
            entity.Property(e => e.Salary).HasMaxLength(150);
            entity.Property(e => e.Skills).HasMaxLength(500);
            entity.Property(e => e.Missions).HasMaxLength(3000);
            entity.Property(e => e.Benefits).HasMaxLength(3000);
            entity.Property(e => e.Process).HasMaxLength(2000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsArchived).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.ContractType);
            entity.HasIndex(e => e.AcademicLevel);
            entity.HasIndex(e => e.PostType);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsArchived);
        });

        // ── JobApplication ────────────────────────────────────────────────────
        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.ToTable("job_applications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.CvFile).HasMaxLength(500);
            entity.Property(e => e.MotivationLetterFile).HasMaxLength(500);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.Status).IsRequired().HasConversion<string>()
                .HasDefaultValue(ApplicationStatus.Nouvelle);
            entity.Property(e => e.InternalNote).HasMaxLength(500);
            entity.Property(e => e.AppliedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.JobOffer)
                .WithMany(o => o.Applications)
                .HasForeignKey(e => e.JobOfferId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.JobOfferId);
            entity.HasIndex(e => e.Status);
        });

        // ── NewsletterSubscriber ──────────────────────────────────────────────
        modelBuilder.Entity<NewsletterSubscriber>(entity =>
        {
            entity.ToTable("newsletter_subscribers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SubscribedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UnsubscribeToken).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.UnsubscribeToken).IsUnique();
        });

        // ── ContactRequest ────────────────────────────────────────────────────
        modelBuilder.Entity<ContactRequest>(entity =>
        {
            entity.ToTable("contact_requests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Company).HasMaxLength(200);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OtherSubject).HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(3000);
            entity.Property(e => e.Status).HasDefaultValue("Nouveau").HasMaxLength(50);
            entity.Property(e => e.InternalNote).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.Website).HasMaxLength(300);
            entity.Property(e => e.Sector).HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.DisplayOrder);
        });

        modelBuilder.Entity<Partner>(entity =>
        {
            entity.ToTable("partners");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.Website).HasMaxLength(300);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.ContactPerson).HasMaxLength(150);
            entity.Property(e => e.ContactEmail).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.PartnerType).HasMaxLength(100);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.DisplayOrder);
        });

        // ── IndustrySector ────────────────────────────────────────────────────
        modelBuilder.Entity<PartnerClaim>(entity =>
        {
            entity.ToTable("partner_claims");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(3000);
            entity.Property(e => e.Priority).HasMaxLength(50).HasDefaultValue("Normale");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Nouvelle");
            entity.Property(e => e.SavNote).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            // UserId → User (partenaire backoffice, rôle "Partenaire")
            // SQL Server limitation: avoid multiple cascade paths to the same principal table.
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedToUser).WithMany().HasForeignKey(e => e.AssignedToUserId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<PartnerDemand>(entity =>
        {
            entity.ToTable("partner_demands");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DemandType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(3000);
            entity.Property(e => e.AttachmentUrl).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Nouvelle");
            entity.Property(e => e.CommercialNote).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            // UserId → User (partenaire backoffice, rôle "Partenaire")
            // SQL Server limitation: avoid multiple cascade paths to the same principal table.
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedToUser).WithMany().HasForeignKey(e => e.AssignedToUserId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<IndustrySector>(entity =>
        {
            entity.ToTable("industry_sectors");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.DisplayOrder);
        });
    }
}