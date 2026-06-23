namespace DeepSearch.Core.Queries;

/// <summary>
/// תוצאת הרצת שאילתה. כוללת גם את הניסוח הקריא (דרישה 2)
/// וגם את שורות הנתונים לטבלה ולגרף (דרישה 3).
/// </summary>
public class QueryResult
{
    /// <summary>הניסוח הקריא של השאלה, למשל: "השכר הממוצע של נשים...".</summary>
    public string ReadablePhrase { get; set; } = string.Empty;

    /// <summary>שמות עמודות הפילוח, למשל ["year"] או ["year","gender"].</summary>
    public List<string> GroupKeys { get; set; } = new();

    /// <summary>שורות התוצאה.</summary>
    public List<QueryResultRow> Rows { get; set; } = new();
}

public class QueryResultRow
{
    /// <summary>ערכי הפילוח לשורה הזו, למשל {"year":"2021"}.</summary>
    public Dictionary<string, string> Groups { get; set; } = new();

    /// <summary>ערך המדד המחושב (הממוצע / הסכום / הכמות).</summary>
    public double Value { get; set; }
}
