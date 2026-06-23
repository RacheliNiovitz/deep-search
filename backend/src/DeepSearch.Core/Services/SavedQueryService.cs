using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Entities;
using DeepSearch.Core.Exceptions;
using DeepSearch.Core.Queries;

namespace DeepSearch.Core.Services;

/// <summary>
/// ניהול שאילתות שמורות (דרישה 4): שמירה, רשימה, והרצה מחדש.
/// את ה-QueryDefinition שומרים כ-JSON, וכשרצים מחדש מפענחים ומריצים דרך QueryService.
/// </summary>
public class SavedQueryService : ISavedQueryService
{
    private readonly ISavedQueryRepository _repository;
    private readonly IQueryService _queryService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }   // enums כטקסט קריא ב-JSON
    };

    public SavedQueryService(ISavedQueryRepository repository, IQueryService queryService)
    {
        _repository = repository;
        _queryService = queryService;
    }

    public async Task<SavedQuery> SaveAsync(string name, QueryDefinition definition, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("חובה לתת שם לשאילתה השמורה.");

        var entity = new SavedQuery
        {
            Name = name.Trim(),
            DefinitionJson = JsonSerializer.Serialize(definition, JsonOptions),
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.AddAsync(entity, ct);
    }

    public Task<List<SavedQuery>> ListAsync(CancellationToken ct = default)
        => _repository.GetAllAsync(ct);

    public async Task<QueryResult> RunSavedAsync(int id, CancellationToken ct = default)
    {
        var saved = await _repository.GetByIdAsync(id, ct)
            ?? throw new ValidationException($"שאילתה שמורה עם מזהה {id} לא נמצאה.");

        var definition = JsonSerializer.Deserialize<QueryDefinition>(saved.DefinitionJson, JsonOptions)
            ?? throw new ValidationException("הגדרת השאילתה השמורה פגומה.");

        return await _queryService.ExecuteAsync(definition, ct);
    }
}
