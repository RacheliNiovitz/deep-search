namespace DeepSearch.Core.Entities;

/// <summary>
/// טבלת העובדות (Fact Table). כל מופע = תצפית על אדם אחד בשנה אחת.
/// על הטבלה הזו מתבצעים כל החישובים (ממוצע / סכום / כמות).
/// </summary>
public class PopulationRecord
{
    public long Id { get; set; }

    public string Gender { get; set; } = string.Empty;   // "male" / "female"
    public int Age { get; set; }

    public int CityId { get; set; }
    public City? City { get; set; }                      // ניווט (Navigation) אל הממד

    public int SectorId { get; set; }
    public Sector? Sector { get; set; }

    public int Year { get; set; }
    public double MonthlyIncome { get; set; }     // double - לאגרגציות חלקות גם ב-SQLite
    public bool IsEmployed { get; set; }
}
