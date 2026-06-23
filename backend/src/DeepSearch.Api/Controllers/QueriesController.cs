using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Queries;
using Microsoft.AspNetCore.Mvc;

namespace DeepSearch.Api.Controllers;

/// <summary>הרצת שאילתה מובנית: מקבל QueryDefinition ומחזיר תוצאה + ניסוח קריא.</summary>
[ApiController]
[Route("api/queries")]
public class QueriesController : ControllerBase
{
    private readonly IQueryService _queryService;

    public QueriesController(IQueryService queryService) => _queryService = queryService;

    [HttpPost("execute")]
    public async Task<ActionResult<QueryResult>> Execute([FromBody] QueryDefinition definition, CancellationToken ct)
        => Ok(await _queryService.ExecuteAsync(definition, ct));
}
