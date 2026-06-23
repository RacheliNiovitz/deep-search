using DeepSearch.Core.Queries;

namespace DeepSearch.Core.Abstractions;

/// <summary>
/// ★ נקודת ההחלפה ל-LLM ★
/// ממיר שאלה בשפה חופשית ל-QueryDefinition מובנה.
/// כרגע ממומש כ-Parser מבוסס חוקים; בעתיד אפשר להחליף ל-LLM אמיתי
/// (GPT/Claude/Gemini) בלי לגעת בשאר המערכת - רק להחליף את הרישום ב-DI.
/// </summary>
public interface INlQueryParser
{
    Task<NlParseResult> ParseAsync(string question, CancellationToken ct = default);
}

/// <summary>תוצאת הפירוש: השאילתה המובנית + הסבר כיצד פורשה השאלה (דרישה 5).</summary>
public class NlParseResult
{
    public QueryDefinition Definition { get; set; } = new();

    /// <summary>"כך הבנתי את השאלה" - מוצג למשתמש לפני ההרצה.</summary>
    public string Interpretation { get; set; } = string.Empty;

    /// <summary>חלקים בשאלה שלא זוהו (שקיפות למשתמש).</summary>
    public List<string> Warnings { get; set; } = new();
}
