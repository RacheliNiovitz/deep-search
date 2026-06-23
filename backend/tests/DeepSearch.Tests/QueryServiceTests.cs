using DeepSearch.Core.Exceptions;
using DeepSearch.Core.Queries;
using DeepSearch.Core.Services;
using Xunit;

namespace DeepSearch.Tests;

/// <summary>בדיקות לשירות המרכזי (QueryService) - בעיקר ולידציה.</summary>
public class QueryServiceTests
{
    private static QueryService CreateService()
        => new(new FakePopulationRepository(), new FakeMetadataRepository(), new QueryPhraseBuilder());

    [Fact]
    public async Task UnknownCityId_ThrowsValidationException()
    {
        var def = new QueryDefinition { Filters = new QueryFilters { CityId = 999 } };

        await Assert.ThrowsAsync<ValidationException>(() => CreateService().ExecuteAsync(def));
    }

    [Fact]
    public async Task AgeMinGreaterThanMax_ThrowsValidationException()
    {
        var def = new QueryDefinition { Filters = new QueryFilters { AgeMin = 40, AgeMax = 20 } };

        await Assert.ThrowsAsync<ValidationException>(() => CreateService().ExecuteAsync(def));
    }

    [Fact]
    public async Task ValidQuery_ReturnsResultWithReadablePhrase()
    {
        var def = new QueryDefinition { Metric = MetricType.Average, Filters = new QueryFilters { CityId = 1 } };

        var result = await CreateService().ExecuteAsync(def);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.ReadablePhrase));
    }
}
