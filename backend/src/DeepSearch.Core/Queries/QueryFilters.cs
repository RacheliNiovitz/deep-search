namespace DeepSearch.Core.Queries;

/// <summary>
/// הסינונים של השאילתה (ה"אוכלוסייה"). כל שדה הוא אופציונלי (nullable) -
/// אם הוא null, לא מסננים לפיו. למשל: AgeMin=25, AgeMax=35 => גילאי 25 עד 35.
/// </summary>
public class QueryFilters
{
    public string? Gender { get; set; }     // "male" / "female" / null
    public int? AgeMin { get; set; }
    public int? AgeMax { get; set; }
    public int? CityId { get; set; }
    public int? DistrictId { get; set; }
    public int? SectorId { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
}
