using DeepSearch.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeepSearch.Infrastructure.Data;

/// <summary>
/// ה-DbContext של EF Core: "הגשר" בין מחלקות C# לבין הטבלאות ב-DB.
/// כל DbSet הוא טבלה. ב-OnModelCreating מגדירים שמות טבלאות, מפתחות וקשרים.
/// </summary>
public class DeepSearchDbContext : DbContext
{
    public DeepSearchDbContext(DbContextOptions<DeepSearchDbContext> options) : base(options) { }

    public DbSet<District> Districts => Set<District>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Sector> Sectors => Set<Sector>();
    public DbSet<PopulationRecord> PopulationRecords => Set<PopulationRecord>();
    public DbSet<SavedQuery> SavedQueries => Set<SavedQuery>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<District>(e =>
        {
            e.ToTable("districts");
            e.HasKey(d => d.Id);
            e.Property(d => d.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(d => d.Name).IsUnique();
        });

        b.Entity<City>(e =>
        {
            e.ToTable("cities");
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(c => c.Name).IsUnique();
            e.HasOne(c => c.District).WithMany().HasForeignKey(c => c.DistrictId);
        });

        b.Entity<Sector>(e =>
        {
            e.ToTable("sectors");
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(s => s.Name).IsUnique();
        });

        b.Entity<PopulationRecord>(e =>
        {
            e.ToTable("population_records");
            e.HasKey(p => p.Id);
            e.Property(p => p.Gender).IsRequired().HasMaxLength(10);

            // קשרים אל טבלאות הממד
            e.HasOne(p => p.City).WithMany().HasForeignKey(p => p.CityId);
            e.HasOne(p => p.Sector).WithMany().HasForeignKey(p => p.SectorId);

            // אינדקסים על העמודות שמסננים/מפלחים לפיהן
            e.HasIndex(p => p.Year);
            e.HasIndex(p => p.CityId);
            e.HasIndex(p => p.Gender);
            e.HasIndex(p => p.SectorId);
        });

        b.Entity<SavedQuery>(e =>
        {
            e.ToTable("saved_queries");
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).IsRequired().HasMaxLength(200);
            e.Property(s => s.DefinitionJson).IsRequired();
        });
    }
}
