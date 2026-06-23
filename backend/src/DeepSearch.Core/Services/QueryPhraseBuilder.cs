using System.Text;
using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Entities;
using DeepSearch.Core.Queries;

namespace DeepSearch.Core.Services;

/// <summary>
/// מתרגם QueryDefinition למשפט קריא בעברית.
/// לדוגמה: "השכר הממוצע של נשים בגילאי 25–35 בירושלים בין השנים 2021–2024, בחלוקה לפי שנה".
/// </summary>
public class QueryPhraseBuilder : IQueryPhraseBuilder
{
    public string Build(QueryDefinition def, IReadOnlyList<City> cities, IReadOnlyList<Sector> sectors,
                        IReadOnlyList<District> districts)
    {
        var sb = new StringBuilder();
        var f = def.Filters;

        // 1. המדד + אוכלוסיית הבסיס (מגדר).
        //    ב-Count משלבים את המגדר ישירות ("כמות הנשים") כדי שהניסוח יהיה טבעי.
        if (def.Metric == MetricType.Count)
        {
            sb.Append("כמות ");
            sb.Append(f.Gender switch
            {
                "female" => "הנשים",
                "male"   => "הגברים",
                _        => "האנשים"
            });
        }
        else
        {
            sb.Append(def.Metric switch
            {
                MetricType.Average        => "השכר הממוצע",
                MetricType.Sum            => "סך השכר",
                MetricType.EmploymentRate => "שיעור התעסוקה",
                _                         => "התוצאה"
            });
            sb.Append(" של ");
            sb.Append(f.Gender switch
            {
                "female" => "נשים",
                "male"   => "גברים",
                _        => "כלל האוכלוסייה"
            });
        }

        // 2. מגזר ("במגזר החרדי")
        if (f.SectorId is int sid)
        {
            var sector = sectors.FirstOrDefault(s => s.Id == sid);
            if (sector != null) sb.Append($" במגזר ה{sector.Name}");
        }

        // 3. גיל
        if (f.AgeMin.HasValue || f.AgeMax.HasValue)
            sb.Append($" בגילאי {f.AgeMin?.ToString() ?? ""}–{f.AgeMax?.ToString() ?? ""}");

        // 4. עיר / מחוז
        if (f.CityId is int cid)
        {
            var city = cities.FirstOrDefault(c => c.Id == cid);
            if (city != null) sb.Append($" ב{city.Name}");
        }
        if (f.DistrictId is int did)
        {
            var district = districts.FirstOrDefault(d => d.Id == did);
            if (district != null) sb.Append($" במחוז {district.Name}");
        }

        // 5. תקופת הזמן
        if (f.YearFrom.HasValue && f.YearTo.HasValue && f.YearFrom != f.YearTo)
            sb.Append($" בין השנים {f.YearFrom}–{f.YearTo}");
        else if (f.YearFrom.HasValue)
            sb.Append($" בשנת {f.YearFrom}");

        // 6. הפילוח
        if (def.GroupBy.Count > 0)
        {
            var parts = def.GroupBy.Select(GroupByText);
            sb.Append($", בחלוקה לפי {string.Join(" ו", parts)}");
        }

        return sb.ToString();
    }

    private static string GroupByText(GroupByField field) => field switch
    {
        GroupByField.Year     => "שנה",
        GroupByField.Gender   => "מגדר",
        GroupByField.City     => "עיר",
        GroupByField.District => "מחוז",
        GroupByField.Sector   => "מגזר",
        GroupByField.AgeGroup => "קבוצת גיל",
        _                     => field.ToString()
    };
}
