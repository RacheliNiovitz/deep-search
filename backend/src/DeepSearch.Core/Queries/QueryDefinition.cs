namespace DeepSearch.Core.Queries;

/// <summary>
/// ★ הלב של המערכת ★
/// תיאור מובנה ומלא של שאלה אחת. גם בונה השאילתות (תפריטים) וגם רכיב ה-LLM
/// מייצרים בדיוק את האובייקט הזה, ומשם ההרצה זהה לחלוטין בשתי הזרימות.
/// </summary>
public class QueryDefinition
{
    /// <summary>איזה מדד לחשב: ממוצע / כמות / סכום.</summary>
    public MetricType Metric { get; set; } = MetricType.Average;

    /// <summary>על איזו עמודה מספרית מחשבים (רלוונטי ל-Average/Sum). Count מתעלם מזה.</summary>
    public string MetricField { get; set; } = "MonthlyIncome";

    /// <summary>הסינונים (האוכלוסייה + טווח השנים).</summary>
    public QueryFilters Filters { get; set; } = new();

    /// <summary>לפי אילו שדות לפלח את התוצאה (יכול להיות ריק = שורת סיכום אחת).</summary>
    public List<GroupByField> GroupBy { get; set; } = new();
}
