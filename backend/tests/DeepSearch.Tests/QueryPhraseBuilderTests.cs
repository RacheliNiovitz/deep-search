using DeepSearch.Core.Queries;
using DeepSearch.Core.Services;
using Xunit;

namespace DeepSearch.Tests;

/// <summary>בדיקות לבונה הניסוח הקריא (QueryPhraseBuilder).</summary>
public class QueryPhraseBuilderTests
{
    private readonly QueryPhraseBuilder _builder = new();
    private static readonly IReadOnlyList<Core.Entities.City> Cities = FakeMetadataRepository.Cities;
    private static readonly IReadOnlyList<Core.Entities.Sector> Sectors = FakeMetadataRepository.Sectors;
    private static readonly IReadOnlyList<Core.Entities.District> Districts = FakeMetadataRepository.Districts;

    [Fact]
    public void Average_WithFullFilters_BuildsExpectedHebrewPhrase()
    {
        var def = new QueryDefinition
        {
            Metric = MetricType.Average,
            Filters = new QueryFilters { Gender = "female", AgeMin = 25, AgeMax = 35, CityId = 1, YearFrom = 2021, YearTo = 2024 },
            GroupBy = new() { GroupByField.Year }
        };

        var phrase = _builder.Build(def, Cities, Sectors, Districts);

        Assert.Equal("השכר הממוצע של נשים בגילאי 25–35 בירושלים בין השנים 2021–2024, בחלוקה לפי שנה", phrase);
    }

    [Fact]
    public void Count_WithGender_ReadsNaturally()
    {
        // "כמות הנשים" ולא "כמות האנשים של נשים"
        var def = new QueryDefinition { Metric = MetricType.Count, Filters = new QueryFilters { Gender = "female", CityId = 2 } };

        var phrase = _builder.Build(def, Cities, Sectors, Districts);

        Assert.StartsWith("כמות הנשים", phrase);
    }

    [Fact]
    public void Sector_IsPhrasedAsBemigzar()
    {
        var def = new QueryDefinition { Metric = MetricType.Average, Filters = new QueryFilters { SectorId = 2 } };

        var phrase = _builder.Build(def, Cities, Sectors, Districts);

        Assert.Contains("במגזר החרדי", phrase);
    }

    [Fact]
    public void EmploymentRate_UsesCorrectPrefix()
    {
        var def = new QueryDefinition { Metric = MetricType.EmploymentRate, GroupBy = new() { GroupByField.City } };

        var phrase = _builder.Build(def, Cities, Sectors, Districts);

        Assert.StartsWith("שיעור התעסוקה", phrase);
    }
}
