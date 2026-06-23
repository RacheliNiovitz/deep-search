using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Entities;
using DeepSearch.Core.Queries;
using DeepSearch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeepSearch.Infrastructure.Repositories;

/// <summary>
/// גישה לטבלת העובדות + ביצוע האגרגציה. מתרגם QueryDefinition ל-LINQ,
/// ש-EF Core מתרגם ל-SQL פרמטרי ובטוח (אין הצמדת מחרוזות => אין SQL Injection).
/// </summary>
public class PopulationRepository : IPopulationRepository
{
    private readonly DeepSearchDbContext _db;

    public PopulationRepository(DeepSearchDbContext db) => _db = db;

    public async Task<List<QueryResultRow>> ExecuteAggregationAsync(QueryDefinition def, CancellationToken ct = default)
    {
        // ----- שלב 1: סינון (WHERE) -----
        // בונים את השאילתה צעד-צעד. כל סינון מתווסף רק אם הוגדר (לא null).
        IQueryable<PopulationRecord> query = _db.PopulationRecords.AsNoTracking();

        var f = def.Filters;
        if (f.Gender is not null)   query = query.Where(r => r.Gender == f.Gender);
        if (f.AgeMin is int amin)   query = query.Where(r => r.Age >= amin);
        if (f.AgeMax is int amax)   query = query.Where(r => r.Age <= amax);
        if (f.CityId is int city)     query = query.Where(r => r.CityId == city);
        if (f.DistrictId is int dist) query = query.Where(r => r.City!.DistrictId == dist);
        if (f.SectorId is int sec)    query = query.Where(r => r.SectorId == sec);
        if (f.YearFrom is int yfrom) query = query.Where(r => r.Year >= yfrom);
        if (f.YearTo is int yto)    query = query.Where(r => r.Year <= yto);

        // ----- שלב 2: פילוח (GROUP BY) דינמי -----
        // אילו שדות לפלח? מה שלא נבחר -> מקבל null קבוע, וכך "מתאחד" לקבוצה אחת.
        bool byYear     = def.GroupBy.Contains(GroupByField.Year);
        bool byGender   = def.GroupBy.Contains(GroupByField.Gender);
        bool byCity     = def.GroupBy.Contains(GroupByField.City);
        bool byDistrict = def.GroupBy.Contains(GroupByField.District);
        bool bySector   = def.GroupBy.Contains(GroupByField.Sector);
        bool byAgeGroup = def.GroupBy.Contains(GroupByField.AgeGroup);

        var grouped = query.GroupBy(r => new
        {
            Year       = byYear     ? (int?)r.Year               : null,
            Gender     = byGender   ? r.Gender                    : null,
            CityId     = byCity     ? (int?)r.CityId              : null,
            DistrictId = byDistrict ? (int?)r.City!.DistrictId    : null,
            SectorId   = bySector   ? (int?)r.SectorId            : null,
            AgeGroup   = byAgeGroup ? (int?)((r.Age / 10) * 10)   : null   // עשור: 24 -> 20, 37 -> 30
        });

        // ----- שלב 3: חישוב המדד (AVG / SUM / COUNT) -----
        // בוחרים את פונקציית האגרגציה לפי המדד שביקש המשתמש.
        List<AggRow> raw = def.Metric switch
        {
            MetricType.Count => await grouped.Select(g => new AggRow
            {
                Year = g.Key.Year, Gender = g.Key.Gender, CityId = g.Key.CityId,
                DistrictId = g.Key.DistrictId, SectorId = g.Key.SectorId, AgeGroup = g.Key.AgeGroup,
                Value = g.Count()
            }).ToListAsync(ct),

            MetricType.Sum => await grouped.Select(g => new AggRow
            {
                Year = g.Key.Year, Gender = g.Key.Gender, CityId = g.Key.CityId,
                DistrictId = g.Key.DistrictId, SectorId = g.Key.SectorId, AgeGroup = g.Key.AgeGroup,
                Value = g.Sum(r => r.MonthlyIncome)
            }).ToListAsync(ct),

            // שיעור תעסוקה = ממוצע של IsEmployed (0/1) כפול 100 => אחוז המועסקים בקבוצה
            MetricType.EmploymentRate => await grouped.Select(g => new AggRow
            {
                Year = g.Key.Year, Gender = g.Key.Gender, CityId = g.Key.CityId,
                DistrictId = g.Key.DistrictId, SectorId = g.Key.SectorId, AgeGroup = g.Key.AgeGroup,
                Value = g.Average(r => r.IsEmployed ? 1.0 : 0.0) * 100
            }).ToListAsync(ct),

            _ => await grouped.Select(g => new AggRow            // Average (ברירת מחדל)
            {
                Year = g.Key.Year, Gender = g.Key.Gender, CityId = g.Key.CityId,
                DistrictId = g.Key.DistrictId, SectorId = g.Key.SectorId, AgeGroup = g.Key.AgeGroup,
                Value = g.Average(r => r.MonthlyIncome)
            }).ToListAsync(ct)
        };

        // ----- שלב 4: המרה לתוצאה קריאה (resolve שמות ערים/מגזרים, תוויות מגדר וגיל) -----
        var cityNames     = await _db.Cities.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        var sectorNames   = await _db.Sectors.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        var districtNames = await _db.Districts.AsNoTracking().ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        var rows = raw
            .OrderBy(r => r.Year).ThenBy(r => r.DistrictId).ThenBy(r => r.CityId)
            .ThenBy(r => r.SectorId).ThenBy(r => r.AgeGroup).ThenBy(r => r.Gender)
            .Select(r =>
            {
                var groups = new Dictionary<string, string>();
                if (byYear     && r.Year is int y)        groups["year"]     = y.ToString();
                if (byGender   && r.Gender is not null)   groups["gender"]   = r.Gender == "female" ? "נשים" : "גברים";
                if (byCity     && r.CityId is int cid)    groups["city"]     = cityNames.GetValueOrDefault(cid, cid.ToString());
                if (byDistrict && r.DistrictId is int did) groups["district"] = districtNames.GetValueOrDefault(did, did.ToString());
                if (bySector   && r.SectorId is int sid2) groups["sector"]   = sectorNames.GetValueOrDefault(sid2, sid2.ToString());
                if (byAgeGroup && r.AgeGroup is int ag)   groups["agegroup"] = $"{ag}-{ag + 9}";

                return new QueryResultRow
                {
                    Groups = groups,
                    Value = Math.Round(r.Value, 2)
                };
            })
            .ToList();

        return rows;
    }

    /// <summary>שורת ביניים שטוחה לקליטת תוצאת ה-GROUP BY מה-DB.</summary>
    private sealed class AggRow
    {
        public int? Year { get; set; }
        public string? Gender { get; set; }
        public int? CityId { get; set; }
        public int? DistrictId { get; set; }
        public int? SectorId { get; set; }
        public int? AgeGroup { get; set; }
        public double Value { get; set; }
    }
}
