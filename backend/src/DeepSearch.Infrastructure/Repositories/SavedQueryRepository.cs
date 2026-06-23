using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Entities;
using DeepSearch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeepSearch.Infrastructure.Repositories;

/// <summary>CRUD לשאילתות שמורות.</summary>
public class SavedQueryRepository : ISavedQueryRepository
{
    private readonly DeepSearchDbContext _db;

    public SavedQueryRepository(DeepSearchDbContext db) => _db = db;

    public async Task<SavedQuery> AddAsync(SavedQuery query, CancellationToken ct = default)
    {
        _db.SavedQueries.Add(query);
        await _db.SaveChangesAsync(ct);
        return query;
    }

    public Task<List<SavedQuery>> GetAllAsync(CancellationToken ct = default)
        => _db.SavedQueries.AsNoTracking()
               .OrderByDescending(q => q.CreatedAt)
               .ToListAsync(ct);

    public Task<SavedQuery?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.SavedQueries.AsNoTracking()
               .FirstOrDefaultAsync(q => q.Id == id, ct);
}
