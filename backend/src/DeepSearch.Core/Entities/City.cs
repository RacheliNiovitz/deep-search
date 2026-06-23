namespace DeepSearch.Core.Entities;

/// <summary>
/// עיר - טבלת ממד. מזינה את רשימת הערים בבונה השאילתות.
/// </summary>
public class City
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int DistrictId { get; set; }
    public District? District { get; set; }     // כל עיר משויכת למחוז
}
