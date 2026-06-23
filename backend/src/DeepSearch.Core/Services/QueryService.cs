using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Entities;
using DeepSearch.Core.Exceptions;
using DeepSearch.Core.Queries;

namespace DeepSearch.Core.Services;

/// <summary>
/// השירות המרכזי של המערכת. מקבל QueryDefinition ומבצע:
/// 1. ולידציה   2. בניית הניסוח הקריא   3. הרצת האגרגציה   4. הרכבת התוצאה.
/// שימי לב: השירות תלוי רק בממשקים (Abstractions), לא במימושים - זה ה-DI/DIP.
/// </summary>
public class QueryService : IQueryService
{
    private readonly IPopulationRepository _population;
    private readonly IMetadataRepository _metadata;
    private readonly IQueryPhraseBuilder _phraseBuilder;

    public QueryService(
        IPopulationRepository population,
        IMetadataRepository metadata,
        IQueryPhraseBuilder phraseBuilder)
    {
        _population = population;
        _metadata = metadata;
        _phraseBuilder = phraseBuilder;
    }

    public async Task<QueryResult> ExecuteAsync(QueryDefinition definition, CancellationToken ct = default)
    {
        Validate(definition);

        // נטען ערים/מגזרים/מחוזות (גם לניסוח הקריא וגם לוולידציית קיום המזהים)
        var cities = await _metadata.GetCitiesAsync(ct);
        var sectors = await _metadata.GetSectorsAsync(ct);
        var districts = await _metadata.GetDistrictsAsync(ct);

        ValidateReferences(definition, cities, sectors, districts);

        var phrase = _phraseBuilder.Build(definition, cities, sectors, districts);
        var rows = await _population.ExecuteAggregationAsync(definition, ct);

        return new QueryResult
        {
            ReadablePhrase = phrase,
            GroupKeys = definition.GroupBy.Select(g => g.ToString().ToLowerInvariant()).ToList(),
            Rows = rows
        };
    }

    /// <summary>ולידציה של חוקיות השאילתה. נכשל מוקדם עם הודעה ברורה.</summary>
    private static void Validate(QueryDefinition def)
    {
        var f = def.Filters;
        var currentYear = DateTime.UtcNow.Year;

        // גיל
        if (f.AgeMin is < 0 or > 120 || f.AgeMax is < 0 or > 120)
            throw new ValidationException("גיל חייב להיות בין 0 ל-120.");
        if (f.AgeMin.HasValue && f.AgeMax.HasValue && f.AgeMin > f.AgeMax)
            throw new ValidationException("גיל מינימלי לא יכול להיות גדול מגיל מקסימלי.");

        // שנים
        if (f.YearFrom is { } yf && (yf < 1900 || yf > currentYear))
            throw new ValidationException($"שנת התחלה חייבת להיות בין 1900 ל-{currentYear}.");
        if (f.YearTo is { } yt && (yt < 1900 || yt > currentYear))
            throw new ValidationException($"שנת סיום חייבת להיות בין 1900 ל-{currentYear}.");
        if (f.YearFrom.HasValue && f.YearTo.HasValue && f.YearFrom > f.YearTo)
            throw new ValidationException("שנת ההתחלה לא יכולה להיות מאוחרת משנת הסיום.");
    }

    /// <summary>
    /// מוודא שמזהי העיר/המחוז/המגזר בסינון אכן קיימים ב-DB,
    /// כדי שלא נחזיר "0 תוצאות" שקט על מזהה שגוי (למשל cityId=999).
    /// </summary>
    private static void ValidateReferences(QueryDefinition def,
        IReadOnlyList<City> cities, IReadOnlyList<Sector> sectors, IReadOnlyList<District> districts)
    {
        var f = def.Filters;

        if (f.CityId is int c && cities.All(x => x.Id != c))
            throw new ValidationException($"עיר עם מזהה {c} אינה קיימת.");
        if (f.DistrictId is int d && districts.All(x => x.Id != d))
            throw new ValidationException($"מחוז עם מזהה {d} אינו קיים.");
        if (f.SectorId is int s && sectors.All(x => x.Id != s))
            throw new ValidationException($"מגזר עם מזהה {s} אינו קיים.");
    }
}
