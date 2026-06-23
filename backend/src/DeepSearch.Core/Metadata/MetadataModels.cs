namespace DeepSearch.Core.Metadata;

/// <summary>
/// כל מה שבונה השאילתות צריך כדי לבנות את עצמו דינמית:
/// רשימת ערים, מגזרים, מגדרים, מדדים אפשריים ושדות פילוח.
/// ה-Frontend טוען את זה פעם אחת ובונה את התפריטים לפיו.
/// </summary>
public class MetadataDto
{
    public List<OptionDto> Cities { get; set; } = new();
    public List<OptionDto> Districts { get; set; } = new();
    public List<OptionDto> Sectors { get; set; } = new();
    public List<OptionDto> Genders { get; set; } = new();
    public List<OptionDto> Metrics { get; set; } = new();
    public List<OptionDto> GroupByFields { get; set; } = new();
}

/// <summary>אפשרות בודדת בתפריט: ערך טכני (Value) + תווית לתצוגה (Label).</summary>
public class OptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    public OptionDto() { }
    public OptionDto(string value, string label)
    {
        Value = value;
        Label = label;
    }
}
