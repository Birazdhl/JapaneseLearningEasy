using JapaneseLearningApp.Models;
using Microsoft.EntityFrameworkCore;

namespace JapaneseLearningApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<JapaneseWord> JapaneseWords => Set<JapaneseWord>();
    public DbSet<AppMetadata> AppMetadata => Set<AppMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JapaneseWord>(e =>
        {
            e.ToTable("JapaneseTable");
            e.Property(x => x.English).HasMaxLength(500);
            e.Property(x => x.Romaji).HasMaxLength(500);
            e.Property(x => x.Japanese).HasMaxLength(500);
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<AppMetadata>(e =>
        {
            e.ToTable("AppMetadata");
            e.HasKey(x => x.Id);
            e.Property(x => x.LastDatabaseImportUtc);
            // Seed singleton row used for "last updated" on the dashboard.
            e.HasData(new AppMetadata { Id = 1, LastDatabaseImportUtc = null });
        });
    }
}
