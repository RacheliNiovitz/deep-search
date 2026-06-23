using DeepSearch.Core.Queries;
using DeepSearch.Core.Services;
using DeepSearch.Infrastructure.Nlp;
using Xunit;

namespace DeepSearch.Tests;

/// <summary>בדיקות לפרסר מבוסס החוקים (RuleBasedNlQueryParser).</summary>
public class RuleBasedNlQueryParserTests
{
    private static RuleBasedNlQueryParser CreateParser()
        => new(new FakeMetadataRepository(), new QueryPhraseBuilder());

    [Fact]
    public async Task Anashim_DoesNotFalselyDetectFemale()
    {
        // הבאג הקלאסי: "אנשים" מכיל "נשים". חייב להישאר ללא מגדר.
        var result = await CreateParser().ParseAsync("כמה אנשים בתל אביב לפי מגדר");

        Assert.Null(result.Definition.Filters.Gender);
        Assert.Equal(MetricType.Count, result.Definition.Metric);
        Assert.Equal(2, result.Definition.Filters.CityId);
        Assert.Contains(GroupByField.Gender, result.Definition.GroupBy);
    }

    [Fact]
    public async Task FullQuery_ParsesAllParameters()
    {
        var result = await CreateParser()
            .ParseAsync("מהו השכר הממוצע של נשים חרדיות בירושלים בין השנים 2021-2024 לפי שנה");

        var d = result.Definition;
        Assert.Equal(MetricType.Average, d.Metric);
        Assert.Equal("female", d.Filters.Gender);
        Assert.Equal(2, d.Filters.SectorId);          // חרדי
        Assert.Equal(1, d.Filters.CityId);            // ירושלים
        Assert.Equal(2021, d.Filters.YearFrom);
        Assert.Equal(2024, d.Filters.YearTo);
        Assert.Contains(GroupByField.Year, d.GroupBy);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task EmploymentRate_WithMultipleBreakdowns()
    {
        var result = await CreateParser()
            .ParseAsync("הצג את שיעור התעסוקה לפי עיר ומגדר בשנים 2020-2024");

        var d = result.Definition;
        Assert.Equal(MetricType.EmploymentRate, d.Metric);
        Assert.Contains(GroupByField.City, d.GroupBy);
        Assert.Contains(GroupByField.Gender, d.GroupBy);
        Assert.Equal(2020, d.Filters.YearFrom);
        Assert.Equal(2024, d.Filters.YearTo);
    }

    [Fact]
    public async Task NewBreakdowns_SectorAndAgeGroup()
    {
        var result = await CreateParser()
            .ParseAsync("שיעור התעסוקה לפי מגזר וקבוצת גיל");

        Assert.Contains(GroupByField.Sector, result.Definition.GroupBy);
        Assert.Contains(GroupByField.AgeGroup, result.Definition.GroupBy);
    }

    [Fact]
    public async Task District_BreakdownIsDistinctFromCity()
    {
        var result = await CreateParser().ParseAsync("שיעור התעסוקה לפי מחוז ומגדר");

        Assert.Contains(GroupByField.District, result.Definition.GroupBy);
        Assert.Contains(GroupByField.Gender, result.Definition.GroupBy);
        Assert.DoesNotContain(GroupByField.City, result.Definition.GroupBy);
    }

    [Fact]
    public async Task Nonsense_ProducesWarning()
    {
        var result = await CreateParser()
            .ParseAsync("כמות האנשים בתל אביב לפי בנהה ביסלי מגדר");

        Assert.Contains(result.Warnings, w => w.Contains("בנהה"));
    }

    [Fact]
    public async Task UnsupportedSector_ProducesWarning()
    {
        var result = await CreateParser().ParseAsync("שכר ממוצע של נשים דתיות בחיפה");

        Assert.Contains(result.Warnings, w => w.Contains("מגזר"));
    }

    [Fact]
    public async Task EnglishInput_WarnsHebrewOnly()
    {
        var result = await CreateParser().ParseAsync("average salary of women");

        Assert.Contains(result.Warnings, w => w.Contains("עברית"));
    }
}
