using System.Text.RegularExpressions;
using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Entities;
using DeepSearch.Core.Queries;

namespace DeepSearch.Infrastructure.Nlp;

/// <summary>
/// מימוש מבוסס-חוקים של פירוש שפה חופשית (Mock ל-LLM).
/// מזהה בטקסט: מדד, מגדר, עיר, מגזר, טווח גילאים, טווח שנים ופילוחים.
/// המטרה אינה NLP מושלם, אלא להדגים את הזרימה ואת נקודת ההחלפה ל-LLM אמיתי.
/// </summary>
public class RuleBasedNlQueryParser : INlQueryParser
{
    private readonly IMetadataRepository _metadata;
    private readonly IQueryPhraseBuilder _phraseBuilder;

    public RuleBasedNlQueryParser(IMetadataRepository metadata, IQueryPhraseBuilder phraseBuilder)
    {
        _metadata = metadata;
        _phraseBuilder = phraseBuilder;
    }

    public async Task<NlParseResult> ParseAsync(string question, CancellationToken ct = default)
    {
        var text = question ?? string.Empty;
        var def = new QueryDefinition();
        var warnings = new List<string>();

        var cities = await _metadata.GetCitiesAsync(ct);
        var sectors = await _metadata.GetSectorsAsync(ct);
        var districts = await _metadata.GetDistrictsAsync(ct);

        // ----- מדד -----
        if (Contains(text, "שיעור"))                    def.Metric = MetricType.EmploymentRate;
        else if (Contains(text, "כמה", "כמות", "מספר")) def.Metric = MetricType.Count;
        else if (Contains(text, "סך", "סכום"))          def.Metric = MetricType.Sum;
        else if (Contains(text, "תעסוק", "מועסק"))      def.Metric = MetricType.EmploymentRate;
        else                                            def.Metric = MetricType.Average;

        // ----- מגדר -----
        // שים לב: "נשים" מופיע גם בתוך "אנשים"! ה-lookbehind שולל התאמה כשלפני "נשים" יש א.
        if (Regex.IsMatch(text, "(?<!א)נשים") || Contains(text, "נשות", "אישה"))
            def.Filters.Gender = "female";
        else if (Contains(text, "גברים", "גבר", "זכרים"))
            def.Filters.Gender = "male";

        // ----- עיר -----
        var city = cities.FirstOrDefault(c => text.Contains(c.Name));
        if (city != null) def.Filters.CityId = city.Id;

        // ----- מחוז -----
        // "מחוז X" מפורש, או שם-מחוז שאינו שם-עיר (כמו "הדרום"), כדי לא להתנגש עם שם עיר.
        var district = districts.FirstOrDefault(d => text.Contains($"מחוז {d.Name}"))
                       ?? districts.FirstOrDefault(d => text.Contains(d.Name) && cities.All(c => c.Name != d.Name));
        if (district != null) def.Filters.DistrictId = district.Id;

        // ----- מגזר -----
        if (Contains(text, "חרד"))      def.Filters.SectorId = sectors.FirstOrDefault(s => s.Name == "חרדי")?.Id;
        else if (Contains(text, "ערב")) def.Filters.SectorId = sectors.FirstOrDefault(s => s.Name == "ערבי")?.Id;

        // מונחי מגזר/דת/לאום שאינם קיימים בנתונים - שקיפות למשתמש במקום התעלמות שקטה
        string[] unsupportedSectors =
        {
            "דתי", "דתיה", "דתיות", "דתיים", "חילוני", "חילונית", "חילונים", "מסורתי", "מסורתית",
            "יהודי", "יהודיה", "יהודיות", "יהודים", "יהודיה",
            "נוצרי", "נוצריה", "נוצרים", "מוסלמי", "מוסלמית", "מוסלמים",
            "דרוזי", "דרוזית", "בדואי", "בדואית", "אשכנזי", "ספרדי"
        };
        if (def.Filters.SectorId is null && Contains(text, unsupportedSectors))
            warnings.Add("המגזר/קבוצה שצוינו אינם קיימים בנתונים. המגזרים הזמינים: כללי, חרדי, ערבי.");

        // ----- טווח שנים (מספרים בני 4 ספרות בטווח 1900–2100) -----
        var years = Regex.Matches(text, @"\b(19|20)\d{2}\b")
                         .Select(m => int.Parse(m.Value))
                         .Where(y => y is >= 1900 and <= 2100)
                         .OrderBy(y => y).ToList();
        if (years.Count >= 2) { def.Filters.YearFrom = years.First(); def.Filters.YearTo = years.Last(); }
        else if (years.Count == 1) { def.Filters.YearFrom = years[0]; def.Filters.YearTo = years[0]; }

        // ----- טווח גילאים (שני מספרים קטנים עם מקף/"עד", ליד המילה "גיל") -----
        var ageMatch = Regex.Match(text, @"גיל\D*?(\d{1,2})\s*(?:-|–|עד)\s*(\d{1,2})");
        if (ageMatch.Success)
        {
            def.Filters.AgeMin = int.Parse(ageMatch.Groups[1].Value);
            def.Filters.AgeMax = int.Parse(ageMatch.Groups[2].Value);
        }

        // ----- פילוחים (GROUP BY) -----
        // מזהים הקשר של פילוח ("לפי" / "חלוקה" / "פילוח"), ואז כל שדה לפי המילה שלו.
        // כך "לפי עיר ומגדר" יזוהה כשני פילוחים. שימי לב: בודקים "שנה" (יחיד) ולא "שנים",
        // כדי לא להתבלבל עם טווח השנים שבסינון (למשל "בשנים 2020-2024").
        bool grouping = Contains(text, "לפי", "חלוקה", "פילוח");
        if (grouping)
        {
            if (Contains(text, "שנה"))                          def.GroupBy.Add(GroupByField.Year);
            if (Contains(text, "מגדר"))                         def.GroupBy.Add(GroupByField.Gender);
            if (Contains(text, "עיר"))                          def.GroupBy.Add(GroupByField.City);
            if (Contains(text, "מחוז"))                         def.GroupBy.Add(GroupByField.District);
            if (Contains(text, "מגזר"))                         def.GroupBy.Add(GroupByField.Sector);
            if (Contains(text, "קבוצת גיל", "קבוצות גיל", "לפי גיל")) def.GroupBy.Add(GroupByField.AgeGroup);
        }

        bool nothingMatched = def.Filters.Gender is null && def.Filters.CityId is null &&
            def.Filters.DistrictId is null && def.Filters.SectorId is null &&
            def.Filters.YearFrom is null && def.GroupBy.Count == 0;

        if (Regex.IsMatch(text, "[a-zA-Z]") && nothingMatched)
            warnings.Add("המערכת תומכת כרגע בשאלות בעברית בלבד.");
        else if (nothingMatched)
            warnings.Add("לא זוהו פרמטרים ברורים בשאלה - מוצגת ברירת מחדל. נסי לנסח בצורה מפורשת יותר.");

        // ----- זיהוי מילים חסרות פשר (שטויות) שלא שייכות לאוצר המילים של המערכת -----
        var unrecognized = DetectUnrecognizedWords(text, cities, sectors, districts);
        if (unrecognized.Count > 0)
            warnings.Add($"מילים שלא זוהו בשאלה: {string.Join(", ", unrecognized)}. ודאי שהשאלה מנוסחת בבירור.");

        var interpretation = _phraseBuilder.Build(def, cities, sectors, districts);

        return new NlParseResult
        {
            Definition = def,
            Interpretation = interpretation,
            Warnings = warnings
        };
    }

    /// <summary>בודק אם הטקסט מכיל לפחות אחת מהמילים.</summary>
    private static bool Contains(string text, params string[] words)
        => words.Any(text.Contains);

    // מילות קישור / שאלה מוכרות (התאמה מדויקת, לאחר הסרת אותיות שימוש)
    private static readonly string[] KnownStopWords =
        { "לפי", "מהו", "מהי", "הצג", "הצגי", "מה", "של", "את", "הכל", "בין", "עד", "או", "גם", "עם", "במה",
          "כמה", "כמות", "סך", "סכום", "ממוצע", "מספר" };

    // שורשים מוכרים של מילות תוכן (התאמה ב-StartsWith - מטפל בנטיות כמו חרדי/חרדיות)
    private static readonly string[] KnownRoots =
    {
        "ממוצע","שכר","הכנס","שנ","גיל","מגדר","מין","עיר","ערי","מחוז","מגזר","אזור",
        "נש","גבר","זכר","תעסוק","מועסק","אנש","תושב","חלוק","פילוח","כלל","סך","מספר","שיעור",
        "חרד","ערב","דתי","יהוד","נוצר","מוסל","דרוז","חילונ","מסורת","אשכנז","ספרד","בדוא"
    };

    /// <summary>
    /// מחזיר רשימת מילים בטקסט שאינן שייכות לאוצר המילים של המערכת
    /// (לא מילת קישור, לא שורש מוכר, לא שם עיר/מגזר ולא מספר).
    /// </summary>
    private static List<string> DetectUnrecognizedWords(string text, IReadOnlyList<City> cities,
        IReadOnlyList<Sector> sectors, IReadOnlyList<District> districts)
    {
        // אוסף השורשים כולל שמות ערים, מגזרים ומחוזות מה-DB
        var roots = new List<string>(KnownRoots) { "מחוז" };
        foreach (var c in cities) roots.AddRange(c.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        foreach (var s in sectors) roots.Add(s.Name);
        foreach (var d in districts) roots.AddRange(d.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        const string prefixes = "והבלכמש";   // אותיות שימוש מובילות נפוצות
        var unknown = new List<string>();

        // פיצול לאסימונים: רק רצפי אותיות עבריות או ספרות
        var tokens = Regex.Split(text, @"[^א-ת0-9]+").Where(t => t.Length > 0);

        foreach (var token in tokens)
        {
            if (IsRecognizedWord(token, roots, prefixes)) continue;
            if (!unknown.Contains(token)) unknown.Add(token);
        }
        return unknown;
    }

    private static bool IsRecognizedWord(string token, List<string> roots, string prefixes)
    {
        if (token.All(char.IsDigit)) return true;   // מספרים
        if (token.Length <= 2) return true;          // מילים קצרות / אותיות שימוש

        // בודקים את המילה כפי שהיא, ואז לאחר הסרת עד 2 אותיות שימוש מובילות
        for (int strip = 0; strip <= 2; strip++)
        {
            if (strip > 0)
            {
                if (token.Length - strip < 2) break;
                if (prefixes.IndexOf(token[strip - 1]) < 0) break;   // אינה אות שימוש - מפסיקים
            }

            var candidate = token.Substring(strip);
            if (KnownStopWords.Contains(candidate)) return true;
            if (roots.Any(r => candidate.StartsWith(r, StringComparison.Ordinal))) return true;
        }
        return false;
    }
}
