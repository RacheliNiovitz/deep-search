using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Entities;
using DeepSearch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeepSearch.Infrastructure.Repositories;

/// <summary>שליפת נתוני הממד (ערים, מגזרים). AsNoTracking כי זו קריאה בלבד.</summary>
public class MetadataRepository : IMetadataRepository
{
    private readonly DeepSearchDbContext _db;

    public MetadataRepository(DeepSearchDbContext db) => _db = db;

    public Task<List<City>> GetCitiesAsync(CancellationToken ct = default)
        => _db.Cities.AsNoTracking().OrderBy(c => c.Id).ToListAsync(ct);

    public Task<List<Sector>> GetSectorsAsync(CancellationToken ct = default)
        => _db.Sectors.AsNoTracking().OrderBy(s => s.Id).ToListAsync(ct);

    public Task<List<District>> GetDistrictsAsync(CancellationToken ct = default)
        => _db.Districts.AsNoTracking().OrderBy(d => d.Id).ToListAsync(ct);
}
