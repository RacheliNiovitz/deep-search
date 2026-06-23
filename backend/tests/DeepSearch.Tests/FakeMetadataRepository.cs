using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Entities;

namespace DeepSearch.Tests;

/// <summary>
/// מימוש מזויף (Fake) של IMetadataRepository לצורך בדיקות - בלי DB אמיתי.
/// מספק רשימת ערים ומגזרים קבועה.
/// </summary>
public class FakeMetadataRepository : IMetadataRepository
{
    public static readonly List<District> Districts = new()
    {
        new() { Id = 1, Name = "ירושלים" },
        new() { Id = 2, Name = "תל אביב" },
        new() { Id = 3, Name = "הדרום" }
    };

    public static readonly List<City> Cities = new()
    {
        new() { Id = 1, Name = "ירושלים", DistrictId = 1 },
        new() { Id = 2, Name = "תל אביב", DistrictId = 2 },
        new() { Id = 3, Name = "חיפה",    DistrictId = 2 },
        new() { Id = 4, Name = "באר שבע", DistrictId = 3 }
    };

    public static readonly List<Sector> Sectors = new()
    {
        new() { Id = 1, Name = "כללי" },
        new() { Id = 2, Name = "חרדי" },
        new() { Id = 3, Name = "ערבי" }
    };

    public Task<List<City>> GetCitiesAsync(CancellationToken ct = default) => Task.FromResult(Cities);
    public Task<List<Sector>> GetSectorsAsync(CancellationToken ct = default) => Task.FromResult(Sectors);
    public Task<List<District>> GetDistrictsAsync(CancellationToken ct = default) => Task.FromResult(Districts);
}
