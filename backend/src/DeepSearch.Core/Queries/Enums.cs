namespace DeepSearch.Core.Queries;

/// <summary>סוג המדד שמחושב (דרישה: לפחות Average / Count / Sum).</summary>
public enum MetricType
{
    Average,
    Count,
    Sum,
    EmploymentRate      // שיעור תעסוקה (% מועסקים) - מבוסס על השדה IsEmployed
}

/// <summary>שדה לפילוח (GROUP BY). דרישה: לפחות לפי שנה / מגדר / עיר.</summary>
public enum GroupByField
{
    Year,
    Gender,
    City,
    District,   // מחוז
    Sector,     // מגזר
    AgeGroup    // קבוצת גיל (עשורים: 20-29, 30-39 ...)
}
