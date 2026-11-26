using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

public sealed class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : DbContext(options)
{
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter = new(
        value => value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcDateTimeConverter = new(
        value => value,
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null);

    public DbSet<PostRecord> Posts => Set<PostRecord>();

    public DbSet<PostLocalizedContentRecord> PostLocalizedContent => Set<PostLocalizedContentRecord>();

    public DbSet<ProjectRecord> Projects => Set<ProjectRecord>();

    public DbSet<ProjectLocalizedContentRecord> ProjectLocalizedContent => Set<ProjectLocalizedContentRecord>();

    public DbSet<ProjectFeatureRecord> ProjectFeatures => Set<ProjectFeatureRecord>();

    public DbSet<ProjectTechnologyRecord> ProjectTechnologies => Set<ProjectTechnologyRecord>();

    public DbSet<LocaleCatalogRecord> LocaleCatalogs => Set<LocaleCatalogRecord>();

    public DbSet<LocaleCatalogEntryRecord> LocaleCatalogEntries => Set<LocaleCatalogEntryRecord>();

    public DbSet<EmailDeliveryPolicyRecord> EmailDeliveryPolicies => Set<EmailDeliveryPolicyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostRecord>(entity =>
        {
            entity.ToTable("Posts");
            entity.HasKey(post => post.Id);
            entity.Property(post => post.Slug)
                .HasMaxLength(80)
                .UseCollation("NOCASE")
                .IsRequired();
            entity.HasIndex(post => post.Slug).IsUnique();
            entity.Property(post => post.Status)
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();
            entity.Property(post => post.Revision)
                .IsConcurrencyToken()
                .IsRequired();
            entity.Property(post => post.CoverImageUrl).HasMaxLength(2_048);
            entity.Property(post => post.TagsJson).HasMaxLength(512).HasDefaultValue("[]").IsRequired();
            entity.Property(post => post.UpdatedAt)
                .HasConversion(UtcDateTimeConverter)
                .IsRequired();
            entity.Property(post => post.PublishedAt)
                .HasConversion(NullableUtcDateTimeConverter);
            entity.HasMany(post => post.LocalizedContent)
                .WithOne(content => content.Post!)
                .HasForeignKey(content => content.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostLocalizedContentRecord>(entity =>
        {
            entity.ToTable("PostLocalizedContent");
            entity.HasKey(content => new { content.PostId, content.Locale });
            entity.Property(content => content.Locale)
                .HasMaxLength(2)
                .IsRequired();
            entity.Property(content => content.Title)
                .HasMaxLength(120)
                .IsRequired();
            entity.Property(content => content.Excerpt)
                .HasMaxLength(320)
                .IsRequired();
            entity.Property(content => content.Body)
                .HasMaxLength(50_000)
                .IsRequired();
            entity.Property(content => content.CoverImageAlt).HasMaxLength(200);
        });

        modelBuilder.Entity<ProjectRecord>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Id).ValueGeneratedOnAdd();
            entity.Property(project => project.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(project => project.ImageReference).HasMaxLength(512).IsRequired();
            entity.Property(project => project.RepositoryUrl).HasMaxLength(2048);
            entity.Property(project => project.LiveUrl).HasMaxLength(2048);
            entity.Property(project => project.Revision).IsConcurrencyToken().IsRequired();
            entity.Property(project => project.UpdatedAt).HasConversion(UtcDateTimeConverter).IsRequired();
            entity.Property(project => project.PublishedAt).HasConversion(NullableUtcDateTimeConverter);
            entity.HasIndex(project => project.SortOrder).IsUnique();
            entity.HasMany(project => project.LocalizedContent)
                .WithOne(content => content.Project!)
                .HasForeignKey(content => content.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(project => project.Technologies)
                .WithOne(technology => technology.Project!)
                .HasForeignKey(technology => technology.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(ProjectSeedData.Projects.Select(seed => seed.ToRecord()));
        });

        modelBuilder.Entity<ProjectLocalizedContentRecord>(entity =>
        {
            entity.ToTable("ProjectLocalizedContent");
            entity.HasKey(content => new {content.ProjectId, content.Locale});
            entity.Property(content => content.Locale).HasMaxLength(2).IsRequired();
            entity.Property(content => content.Title).HasMaxLength(120).IsRequired();
            entity.Property(content => content.Summary).HasMaxLength(320).IsRequired();
            entity.Property(content => content.Details).HasMaxLength(10_000).IsRequired();
            entity.Property(content => content.FeatureHeading).HasMaxLength(120).IsRequired();
            entity.HasMany(content => content.Features)
                .WithOne(feature => feature.Content!)
                .HasForeignKey(feature => new {feature.ProjectId, feature.Locale})
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(ProjectSeedData.Projects.SelectMany(seed => seed.ToLocalizedContentRecords()));
        });

        modelBuilder.Entity<ProjectFeatureRecord>(entity =>
        {
            entity.ToTable("ProjectFeatures");
            entity.HasKey(feature => new {feature.ProjectId, feature.Locale, feature.Position});
            entity.Property(feature => feature.Value).HasMaxLength(500).IsRequired();
            entity.HasData(ProjectSeedData.Projects.SelectMany(seed => seed.ToFeatureRecords()));
        });

        modelBuilder.Entity<ProjectTechnologyRecord>(entity =>
        {
            entity.ToTable("ProjectTechnologies");
            entity.HasKey(technology => new {technology.ProjectId, technology.Position});
            entity.Property(technology => technology.Value).HasMaxLength(60).IsRequired();
            entity.HasData(ProjectSeedData.Projects.SelectMany(seed => seed.ToTechnologyRecords()));
        });

        modelBuilder.Entity<LocaleCatalogRecord>(entity =>
        {
            entity.ToTable("LocaleCatalogs");
            entity.HasKey(catalog => catalog.Id);
            entity.Property(catalog => catalog.Revision).IsConcurrencyToken().IsRequired();
            entity.Property(catalog => catalog.UpdatedAt).HasConversion(UtcDateTimeConverter).IsRequired();
            entity.HasMany(catalog => catalog.Entries)
                .WithOne(entry => entry.Catalog!)
                .HasForeignKey(entry => entry.CatalogId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LocaleCatalogEntryRecord>(entity =>
        {
            entity.ToTable("LocaleCatalogEntries");
            entity.HasKey(entry => new {entry.CatalogId, entry.Key, entry.Locale});
            entity.Property(entry => entry.Key).HasMaxLength(96).IsRequired();
            entity.Property(entry => entry.Locale).HasMaxLength(2).IsRequired();
            entity.Property(entry => entry.ValueType).HasMaxLength(16).IsRequired();
            entity.Property(entry => entry.ScalarValue).HasMaxLength(2_000);
            entity.Property(entry => entry.ListValueJson).HasMaxLength(8_192);
        });

        modelBuilder.Entity<EmailDeliveryPolicyRecord>(entity =>
        {
            entity.ToTable("EmailDeliveryPolicies");
            entity.HasKey(policy => policy.Id);
            entity.Property(policy => policy.Id).ValueGeneratedNever();
            entity.Property(policy => policy.RecipientAddress).HasMaxLength(320).IsRequired();
            entity.Property(policy => policy.SenderDisplayName).HasMaxLength(120).IsRequired();
            entity.Property(policy => policy.SubjectPrefix).HasMaxLength(120).IsRequired();
            entity.Property(policy => policy.Revision).IsConcurrencyToken().IsRequired();
        });
    }
}
