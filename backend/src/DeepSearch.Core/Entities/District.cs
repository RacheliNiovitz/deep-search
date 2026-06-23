namespace DeepSearch.Core.Entities;

/// <summary>
/// מחוז - טבלת ממד ברמה גבוהה מעיר. כל עיר משויכת למחוז אחד.
/// מאפשר סינון ופילוח ברמת המחוז (לדוגמה: "שיעור התעסוקה לפי מחוז").
/// </summary>
public class District
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
