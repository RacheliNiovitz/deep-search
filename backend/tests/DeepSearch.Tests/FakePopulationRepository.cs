using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Queries;

namespace DeepSearch.Tests;

/// <summary>מימוש מזויף של IPopulationRepository - מחזיר תוצאה ריקה (אין צורך ב-DB).</summary>
public class FakePopulationRepository : IPopulationRepository
{
    public Task<List<QueryResultRow>> ExecuteAggregationAsync(QueryDefinition definition, CancellationToken ct = default)
        => Task.FromResult(new List<QueryResultRow>());
}
