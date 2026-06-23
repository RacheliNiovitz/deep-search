using DeepSearch.Api.Models;
using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Queries;
using Microsoft.AspNetCore.Mvc;

namespace DeepSearch.Api.Controllers;

/// <summary>ניהול שאילתות שמורות (דרישה 4): שמירה, רשימה, והרצה מחדש.</summary>
[ApiController]
[Route("api/saved-queries")]
public class SavedQueriesController : ControllerBase
{
    private readonly ISavedQueryService _savedQueries;

    public SavedQueriesController(ISavedQueryService savedQueries) => _savedQueries = savedQueries;

    /// <summary>שמירת שאילתה חדשה.</summary>
    [HttpPost]
    public async Task<ActionResult> Save([FromBody] SaveQueryRequest request, CancellationToken ct)
    {
        var saved = await _savedQueries.SaveAsync(request.Name, request.Definition, ct);
        return Ok(new { saved.Id, saved.Name, saved.CreatedAt });
    }

    /// <summary>רשימת השאילתות השמורות.</summary>
    [HttpGet]
    public async Task<ActionResult> List(CancellationToken ct)
    {
        var items = await _savedQueries.ListAsync(ct);
        return Ok(items.Select(q => new { q.Id, q.Name, q.CreatedAt }));
    }

    /// <summary>הרצה מחדש של שאילתה שמורה לפי מזהה.</summary>
    [HttpPost("{id:int}/run")]
    public async Task<ActionResult<QueryResult>> Run(int id, CancellationToken ct)
        => Ok(await _savedQueries.RunSavedAsync(id, ct));
}
