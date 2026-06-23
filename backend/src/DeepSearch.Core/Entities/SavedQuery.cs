namespace DeepSearch.Core.Entities;

/// <summary>
/// שאילתה שמורה (דרישה 4). את הגדרת השאילתה עצמה שומרים כ-JSON,
/// כדי שנוכל לטעון אותה מחדש ולהריץ שוב.
/// </summary>
public class SavedQuery
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = string.Empty;   // QueryDefinition מסודר כ-JSON
    public DateTime CreatedAt { get; set; }
}
