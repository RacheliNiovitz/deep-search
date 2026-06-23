using DeepSearch.Core.Entities;
using DeepSearch.Core.Queries;

namespace DeepSearch.Core.Abstractions;

/// <summary>
/// גישה לטבלת העובדות. אחראי על הסינון והאגרגציה מול ה-DB.
/// המימוש (ב-Infrastructure) מתרגם QueryDefinition ל-LINQ/SQL בטוח.
/// </summary>
public interface IPopulationRepository
{
    Task<List<QueryResultRow>> ExecuteAggregationAsync(QueryDefinition definition, CancellationToken ct = default);
}

/// <summary>גישה לנתוני הממד (ערים, מגזרים) - מזין את ה-Metadata.</summary>
public interface IMetadataRepository
{
    Task<List<City>> GetCitiesAsync(CancellationToken ct = default);
    Task<List<Sector>> GetSectorsAsync(CancellationToken ct = default);
    Task<List<District>> GetDistrictsAsync(CancellationToken ct = default);
}

/// <summary>CRUD בסיסי לשאילתות שמורות (דרישה 4).</summary>
public interface ISavedQueryRepository
{
    Task<SavedQuery> AddAsync(SavedQuery query, CancellationToken ct = default);
    Task<List<SavedQuery>> GetAllAsync(CancellationToken ct = default);
    Task<SavedQuery?> GetByIdAsync(int id, CancellationToken ct = default);
}
