using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Entities;
using DeepSearch.Core.Queries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DeepSearch.Infrastructure.Nlp;

/// <summary>
/// מימוש INlQueryParser מבוסס LLM אמיתי (Google Gemini).
/// שולח את השאלה החופשית + תיאור הסכמה + רשימת הערים/מגזרים, ומקבל בחזרה
/// QueryDefinition כ-JSON. אם משהו נכשל (אין מפתח / שגיאת רשת / JSON לא תקין) -
/// נופל בחזרה ל-Parser מבוסס החוקים. כך המעבר ל-LLM אמיתי הוא "תוסף", לא סיכון.
/// </summary>
public class GeminiNlQueryParser : INlQueryParser
{
    private readonly HttpClient _http;
    private readonly IMetadataRepository _metadata;
    private readonly IQueryPhraseBuilder _phraseBuilder;
    private readonly RuleBasedNlQueryParser _fallback;
    private readonly ILogger<GeminiNlQueryParser> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public GeminiNlQueryParser(
        HttpClient http,
        IMetadataRepository metadata,
        IQueryPhraseBuilder phraseBuilder,
        RuleBasedNlQueryParser fallback,
        IConfiguration config,
        ILogger<GeminiNlQueryParser> logger)
    {
        _http = http;
        _metadata = metadata;
        _phraseBuilder = phraseBuilder;
        _fallback = fallback;
        _logger = logger;
        _apiKey = config["Gemini:ApiKey"] ?? "";
        _model = config["Gemini:Model"] ?? "gemini-2.0-flash";
    }

    public async Task<NlParseResult> ParseAsync(string question, CancellationToken ct = default)
    {
        var cities = await _metadata.GetCitiesAsync(ct);
        var sectors = await _metadata.GetSectorsAsync(ct);
        var districts = await _metadata.GetDistrictsAsync(ct);

        try
        {
            var prompt = BuildPrompt(question, cities, sectors, districts);
            var rawJson = await CallGeminiAsync(prompt, ct);

            var def = JsonSerializer.Deserialize<QueryDefinition>(rawJson, JsonOptions)
                      ?? throw new InvalidOperationException("Gemini returned empty definition.");

            // הניסוח הקריא נבנה ע"י אותו רכיב משותף - עקביות מול הזרימה המובנית.
            var interpretation = _phraseBuilder.Build(def, cities, sectors, districts);

            return new NlParseResult { Definition = def, Interpretation = interpretation };
        }
        catch (Exception ex)
        {
            // נפילה חזרה לפרסר החוקים - המשתמש תמיד מקבל תשובה.
            _logger.LogWarning(ex, "Gemini parse failed; falling back to rule-based parser.");
            var fallbackResult = await _fallback.ParseAsync(question, ct);
            fallbackResult.Warnings.Add("שירות ה-AI אינו זמין כרגע; נעשה שימוש בפירוש מבוסס חוקים.");
            return fallbackResult;
        }
    }

    /// <summary>בונה את ההנחיה (Prompt) ל-LLM כולל הסכמה ורשימות הערכים החוקיים.</summary>
    private static string BuildPrompt(string question, IReadOnlyList<City> cities, IReadOnlyList<Sector> sectors,
                                      IReadOnlyList<District> districts)
    {
        var cityList = string.Join("\n", cities.Select(c => $"  {c.Id} = {c.Name}"));
        var sectorList = string.Join("\n", sectors.Select(s => $"  {s.Id} = {s.Name}"));
        var districtList = string.Join("\n", districts.Select(d => $"  {d.Id} = {d.Name}"));

        return $$"""
        You convert a Hebrew natural-language question about population statistics into a JSON query definition.
        Return ONLY valid JSON (no markdown, no explanation) with EXACTLY this shape:

        {
          "metric": "Average" | "Count" | "Sum" | "EmploymentRate",
          "metricField": "MonthlyIncome",
          "filters": {
            "gender": "male" | "female" | null,
            "ageMin": number | null,
            "ageMax": number | null,
            "cityId": number | null,
            "districtId": number | null,
            "sectorId": number | null,
            "yearFrom": number | null,
            "yearTo": number | null
          },
          "groupBy": array of any of ["Year","Gender","City","District","Sector","AgeGroup"]
        }

        Rules:
        - metric: "ממוצע/שכר ממוצע" -> Average, "כמה/כמות/מספר" -> Count, "סך/סכום" -> Sum, "שיעור תעסוקה" -> EmploymentRate.
        - Map city names to cityId using ONLY this list (null if not present):
        {{cityList}}
        - Map district (מחוז) names to districtId using ONLY this list (null if not present):
        {{districtList}}
        - Map sector names to sectorId using ONLY this list (null if not present):
        {{sectorList}}
        - gender: "נשים" -> female, "גברים" -> male.
        - Year range like "2021-2024" or "בין השנים 2021 ל-2024" -> yearFrom/yearTo. A single year -> both equal.
        - Age range like "בגילאי 25-35" -> ageMin/ageMax.
        - groupBy from "לפי X": שנה->Year, מגדר->Gender, עיר->City, מחוז->District, מגזר->Sector, קבוצת גיל->AgeGroup.
        - Use null for anything not mentioned. Do NOT invent values.

        Question: "{{question}}"
        """;
    }

    /// <summary>קורא ל-Gemini API ומחזיר את מחרוזת ה-JSON שהמודל הפיק.</summary>
    private async Task<string> CallGeminiAsync(string prompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Gemini API key is not configured.");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0, responseMimeType = "application/json" }
        };
        var payload = JsonSerializer.Serialize(requestBody);

        // ניסיון חוזר על שגיאות זמניות (503/429/5xx) לפני נפילה ל-fallback.
        HttpResponseMessage response;
        int attempt = 0;
        while (true)
        {
            attempt++;
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            response = await _http.PostAsync(url, content, ct);

            if (response.IsSuccessStatusCode) break;

            var code = (int)response.StatusCode;
            bool transient = code is 429 or 500 or 502 or 503 or 504;
            if (!transient || attempt >= 3)
                break;

            response.Dispose();
            await Task.Delay(400 * attempt, ct);   // backoff קצר
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            // חילוץ הטקסט: candidates[0].content.parts[0].text
            using var doc = JsonDocument.Parse(responseJson);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? throw new InvalidOperationException("Gemini response had no text.");
        }
    }
}
