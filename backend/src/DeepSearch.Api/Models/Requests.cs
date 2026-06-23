using DeepSearch.Core.Queries;

namespace DeepSearch.Api.Models;

/// <summary>בקשת שמירת שאילתה: שם + הגדרת השאילתה.</summary>
public record SaveQueryRequest(string Name, QueryDefinition Definition);

/// <summary>בקשת פירוש שאלה חופשית.</summary>
public record NlQueryRequest(string Question);
