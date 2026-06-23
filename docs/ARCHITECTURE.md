# ארכיטקטורה – Deep Search

מסמך זה מפרט את מבנה המערכת, חלוקת האחריות בין הרכיבים, וזרימת המידע.

---

## 1. עקרונות מנחים

1. **הפרדת שכבות (Layered Architecture)** — כל שכבה אחראית על דבר אחד ומדברת רק עם השכבה שמתחתיה.
2. **Dependency Inversion** — הליבה (`Core`) מגדירה *ממשקים* ("מה צריך לקרות"), וה־`Infrastructure` מספק *מימוש* ("איך"). הליבה אינה תלויה בכלום.
3. **נקודת אמת אחת — `QueryDefinition`** — כל מקורות הקלט (תפריטים, LLM) מתכנסים לאובייקט מובנה אחד, וממנו ההרצה זהה.
4. **יכולת החלפה (Swappability)** — ניתן להחליף בסיס נתונים או מנוע LLM ללא שינוי בליבה.

---

## 2. מבנה השכבות (צד שרת)

```
┌────────────────────────────────────────────────────────────┐
│  DeepSearch.Api            (שכבת מצגת / HTTP)               │
│  • Controllers  • Middleware לטיפול בשגיאות  • הגדרות DI    │
│  תלוי ב: Core, Infrastructure                              │
├────────────────────────────────────────────────────────────┤
│  DeepSearch.Core           (לב המערכת - טהור)              │
│  • Entities  • QueryDefinition  • ממשקים (Abstractions)    │
│  • Services: QueryService, MetadataService,               │
│    SavedQueryService, QueryPhraseBuilder                  │
│  תלוי ב: כלום                                              │
├────────────────────────────────────────────────────────────┤
│  DeepSearch.Infrastructure (מימוש)                         │
│  • DeepSearchDbContext (EF Core)  • Repositories           │
│  • RuleBasedNlQueryParser  • GeminiNlQueryParser           │
│  תלוי ב: Core                                              │
└────────────────────────────────────────────────────────────┘
```

**כיוון התלות** תמיד פנימה — אל ה־`Core`. כך, החלפת DB או LLM (ב־Infrastructure) אינה נוגעת בלוגיקה העסקית.

### חלוקת אחריות

| רכיב | אחריות |
|------|--------|
| **Controllers** | קבלת HTTP, ולידציית מבנה, העברה ל־Service. ללא לוגיקה עסקית. |
| **Services** (Core) | לוגיקה עסקית: ולידציה, בניית ניסוח, תיאום הרצה. |
| **Repositories** (Infra) | הגישה היחידה לנתונים. תרגום `QueryDefinition` ל־SQL בטוח. |
| **DbContext** (Infra) | מיפוי אובייקטים↔טבלאות, יצירת SQL פרמטרי. |
| **NL Parsers** (Infra) | תרגום שפה חופשית ל־`QueryDefinition`. |

---

## 3. זרימות המידע

### זרימה א' — שאילתה מובנית
```
משתמש בוחר פרמטרים (Angular)
   → POST /api/queries/execute  { QueryDefinition }
   → QueriesController
   → QueryService: Validate → PhraseBuilder → Repository
   → PopulationRepository: LINQ → SQL פרמטרי (GROUP BY דינמי)
   → DB → אגרגציה
   → QueryResult { ניסוח קריא, GroupKeys, Rows }
   → Angular: טבלה + גרף
```

### זרימה ב' — שאלה חופשית (LLM)
```
משתמש כותב שאלה חופשית (Angular)
   → POST /api/nlp/parse  { question }
   → NlpController → INlQueryParser
        ├─ RuleBasedNlQueryParser  (ברירת מחדל)
        └─ GeminiNlQueryParser     (LLM אמיתי, לפי config)
   → NlParseResult { QueryDefinition, פירוש קריא, אזהרות }
   → המשתמש מאשר → ממשיך לזרימה א' (ההרצה זהה)
```

**שתי הזרימות מתכנסות ל־`QueryDefinition` ולמנוע הרצה אחד.** זה הלב של הארכיטקטורה.

```
  תפריטים  ─┐
            ├─►  QueryDefinition  ──►  QueryService ──► Repository ──► DB
  LLM/NLP  ─┘     (מבנה אחיד)         (מנוע הרצה משותף)
```

---

## 4. רכיב ה־AI/LLM — נקודת ההחלפה

הממשק `INlQueryParser` הוא "החוזה". שני מימושים:

```
INlQueryParser
   ├─ RuleBasedNlQueryParser   → זיהוי מבוסס regex/חוקים (עברית)
   └─ GeminiNlQueryParser      → Google Gemini, עם fallback ל-RuleBased
```

הבחירה נעשית ב־`DependencyInjection.AddInfrastructure` לפי `Nlp:Provider` ב־config. **החלפה = שינוי שורת config אחת.** שאר המערכת (Controllers, Services) אינה יודעת איזה מימוש פעיל.

`GeminiNlQueryParser` בונה Prompt הכולל את הסכמה ואת רשימת הערים/מגזרים החוקיים, מבקש מ־Gemini JSON בלבד (`responseMimeType: application/json`), וממיר אותו ל־`QueryDefinition`. בכל כשל — נפילה חזרה לפרסר החוקים, כך שהמשתמש תמיד מקבל תשובה.

---

## 5. תכנון בסיס הנתונים

```
┌───────────┐    ┌─────────────┐    ┌──────────────────────────┐    ┌───────────┐
│ districts │◄───│   cities    │◄───│   population_records     │───►│  sectors  │
│ id, name  │    │ id, name,   │city│  id, gender, age, year,  │sec │ id, name  │
└───────────┘    │ district_id │_id │  monthly_income,         │tor └───────────┘
                 └─────────────┘    │  is_employed             │_id
                       └──────────────────────────┘
                       (Fact Table - טבלת העובדות)

┌────────────────────────────────────────┐
│  saved_queries                         │
│  id, name, definition (JSON), created  │
└────────────────────────────────────────┘
```

- **`population_records`** — טבלת עובדות (Fact Table). כל שורה = תצפית על אדם בשנה. עליה מתבצעות כל האגרגציות.
- **`districts`, `cities`, `sectors`** — טבלאות ממד (Metadata) בהיררכיה: כל עיר משויכת למחוז. מזינות את תפריטי בונה השאילתות ומבטיחות נורמליזציה. פילוח לפי מחוז מתבצע דרך ה-JOIN עיר→מחוז.
- **`saved_queries`** — שמירת `QueryDefinition` כ־JSON, להרצה חוזרת.
- **אינדקסים** על העמודות שמסננים/מפלחים לפיהן (year, city_id, gender, sector_id) לשיפור ביצועים.

**פילוחים נתמכים:** שנה, מגדר, עיר, מחוז, מגזר, קבוצת גיל. הצירוף נבנה דינמית ב-`PopulationRepository` (GROUP BY אחד שמטפל בכל הצירופים). כשנבחרים שני ממדים, ה-Frontend מציג טבלת צומת (Pivot) וגרף מקובץ בצבעים.

**שני בסיסי נתונים?** לא. בסיס נתונים רלציוני יחיד (SQLite בפיתוח, PostgreSQL בפרודקשן). EF Core מפשט את ההבדל — אותו קוד, החלפת ספק בלבד.

---

## 6. טיפול בשגיאות

`ErrorHandlingMiddleware` תופס את כל החריגות במקום אחד:
- `ValidationException` (מ־Core) → **400 Bad Request** עם הודעה ברורה למשתמש.
- כל חריגה אחרת → **500** + לוג מלא (השגיאה אינה "נבלעת").

כך ה־Controllers נשארים נקיים, וההתנהגות אחידה בכל ה־API.

---

## 7. אבטחה

| נושא | מימוש |
|------|-------|
| **SQL Injection** | כל הגישה לנתונים דרך EF Core עם שאילתות פרמטריות (LINQ → SQL פרמטרי). אין הצמדת מחרוזות לשאילתות. |
| **ולידציית קלט** | בשתי שכבות — בלקוח (UX) ובשרת (`QueryService`): טווחי גיל/שנים, ובדיקת **קיום** מזהי עיר/מחוז/מגזר ב-DB (מזהה שגוי → 400, לא "0 תוצאות" שקט). |
| **CORS** | מדיניות מוגדרת (`Cors:AllowedOrigins`) המגבילה את ה-Origins המורשים. בפריסת השירות-היחיד אין צורך ב-CORS (origin זהה). |
| **ניהול Secrets** | מפתח ה-LLM ומחרוזת החיבור לעולם לא בקוד/Git — `dotnet user-secrets` בפיתוח, **GCP Secret Manager** / env vars בפרודקשן. |
| **חשיפת מידע** | ה-API מחזיר רק נתונים מצרפיים (אגרגציות), לא רשומות אישיות גולמיות. |
| **טיפול בשגיאות** | חריגות לא צפויות מוחזרות כ-500 גנרי (ללא חשיפת פרטים פנימיים), עם לוג מלא בצד השרת. |

> בהקשר של מערכת ממשלתית, בפרודקשן יתווספו אימות/הרשאות (OAuth/OIDC), הצפנת תעבורה (TLS — מסופק ע"י Cloud Run), ו-Audit Logging.

---

## 8. החלטות תכנון מרכזיות (לסיכום)

| החלטה | נימוק |
|-------|-------|
| 3 פרויקטים נפרדים לשכבות | הפרדה *פיזית* — פרויקט לא יכול לגשת למה שאסור לו. |
| `QueryDefinition` כנקודת מפגש | מאחד תפריטים ו־LLM למנוע הרצה אחד. |
| `INlQueryParser` עם 2 מימושים | מוכיח שהארכיטקטורה תומכת ב־LLM אמיתי ללא שינוי ליבה. |
| EF Core + Repository | מונע SQL Injection, מפריד גישת נתונים, מאפשר החלפת DB. |
| GROUP BY דינמי בקוד אחד | תומך בכל צירוף פילוחים (שנה/מגדר/עיר/מגזר/גיל) ללא כפילות קוד. |
