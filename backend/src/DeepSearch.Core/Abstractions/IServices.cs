using DeepSearch.Core.Entities;
using DeepSearch.Core.Metadata;
using DeepSearch.Core.Queries;

namespace DeepSearch.Core.Abstractions;

/// <summary>בונה את הניסוח הקריא של השאילתה (דרישה 2). אחריות יחידה.</summary>
public interface IQueryPhraseBuilder
{
    string Build(QueryDefinition definition, IReadOnlyList<City> cities, IReadOnlyList<Sector> sectors,
                 IReadOnlyList<District> districts);
}

/// <summary>השירות המרכזי: מקבל QueryDefinition, מאמת, מריץ ומחזיר תוצאה + ניסוח.</summary>
public interface IQueryService
{
    Task<QueryResult> ExecuteAsync(QueryDefinition definition, CancellationToken ct = default);
}

/// <summary>מספק את ה-Metadata לבונה השאילתות.</summary>
public interface IMetadataService
{
    Task<MetadataDto> GetMetadataAsync(CancellationToken ct = default);
}

/// <summary>ניהול שאילתות שמורות (שמירה / רשימה / טעינה והרצה מחדש).</summary>
public interface ISavedQueryService
{
    Task<SavedQuery> SaveAsync(string name, QueryDefinition definition, CancellationToken ct = default);
    Task<List<SavedQuery>> ListAsync(CancellationToken ct = default);
    Task<QueryResult> RunSavedAsync(int id, CancellationToken ct = default);
}
