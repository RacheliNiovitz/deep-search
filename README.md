# Deep Search – תשאול דינמי של נתונים ממשלתיים

מערכת **Proof of Concept** עבור הלמ"ס, המאפשרת לעובדי ממשלה לבצע תשאולים דינמיים על מאגר נתונים מרכזי (תעסוקה, השכלה, אוכלוסייה, גיאוגרפיה) — **ללא כתיבת SQL**, באמצעות בונה שאילתות ויזואלי וגם באמצעות שאלה בשפה חופשית (LLM).

> המטרה אינה מערכת Production מלאה, אלא הדגמת תכנון, ארכיטקטורה, איכות קוד וחשיבה מערכתית מקצה לקצה.

## 🌐 הדגמה חיה
**https://deep-search-276124011170.me-west1.run.app** — פרוס על GCP Cloud Run (אזור תל אביב), כולל רכיב ה־LLM האמיתי.
*(הבקשה הראשונה אחרי חוסר פעילות עשויה להיות איטית — cold start.)*

---

## תוכן עניינים
- [יכולות עיקריות](#יכולות-עיקריות)
- [סטאק טכנולוגי ונימוקים](#סטאק-טכנולוגי-ונימוקים)
- [מבנה הפרויקט](#מבנה-הפרויקט)
- [הוראות הרצה](#הוראות-הרצה)
- [סקירת ארכיטקטורה](#סקירת-ארכיטקטורה)
- [חיבור ל־LLM אמיתי (Gemini)](#חיבור-ל־llm-אמיתי-gemini)
- [בדיקות](#בדיקות)
- [הנחות עבודה ומגבלות](#הנחות-עבודה-ומגבלות)
- [מסמכים נוספים](#מסמכים-נוספים)

---

## יכולות עיקריות

1. **בונה שאילתות ויזואלי** — בחירת מדד, אוכלוסייה (מגדר, גיל, עיר, מגזר), טווח שנים ופילוחים.
2. **ניסוח קריא** — המערכת מנסחת את השאלה במשפט עברי טבעי לפני ההרצה.
3. **תוצאות** — טבלה + גרף עמודות.
4. **שאילתות שמורות** — שמירה, רשימה והרצה מחדש.
5. **שאלה בשפה חופשית (LLM)** — הקלדת שאלה חופשית, הצגת פירוש המערכת עם חיווי ויזואלי, והרצה.
6. **מדדים**: ממוצע שכר, כמות, סכום, **שיעור תעסוקה**.
7. **פילוחים**: שנה, מגדר, עיר, **מחוז**, מגזר, קבוצת גיל.
8. **תצוגת צומת (Pivot)** — בפילוח לפי שני ממדים, התוצאה מוצגת כטבלה מקובצת וגרף עמודות בכמה צבעים.

---

## סטאק טכנולוגי ונימוקים

| שכבה | בחירה | נימוק |
|------|-------|-------|
| צד שרת | **ASP.NET Core Web API (.NET 8, C#)** | פלטפורמה בוגרת עם DI מובנה, ביצועים גבוהים, ותמיכה מצוינת בהפרדת שכבות. נבחרה כדי להדגים ארכיטקטורה נקייה ומובנית. |
| גישה לנתונים | **Entity Framework Core** | ORM שמפריד את הלוגיקה מה־DB, מונע SQL Injection (שאילתות פרמטריות), ומאפשר החלפת ספק DB בשורה אחת. |
| בסיס נתונים | **SQLite** (פיתוח) / **PostgreSQL** (פרודקשן) | SQLite — אפס תשתית, רץ מיידית. בפרודקשן עוברים ל־PostgreSQL ב־**GCP Cloud SQL** ע"י החלפת ספק EF Core בלבד. קובץ `database/01_schema.sql` מוכן לפריסת PostgreSQL. |
| Frontend | **Angular 22** (standalone components + signals) | דרישת המטלה. ארכיטקטורה מודרנית עם הפרדה בין קומפוננטות לשירותים. |
| רכיב AI/LLM | **Parser מבוסס חוקים** + **אינטגרציית Google Gemini** | שני מימושים מאחורי אותו ממשק `INlQueryParser` — ניתן להחלפה דרך config בלבד. |

**עקרון מנחה:** כל בחירה נעשתה כדי להדגיש **הפרדת אחריות** ו**יכולת החלפה** (DB, LLM) ללא שינוי בליבת המערכת.

---

## מבנה הפרויקט

```
deep-search/
├── backend/                       # צד שרת (.NET 8)
│   ├── DeepSearch.sln
│   ├── src/
│   │   ├── DeepSearch.Core/           # ליבה: ישויות, QueryDefinition, ממשקים, לוגיקה עסקית
│   │   ├── DeepSearch.Infrastructure/ # מימושים: EF Core, Repositories, NL Parsers
│   │   └── DeepSearch.Api/            # שכבת HTTP: Controllers, DI, Middleware
│   └── tests/
│       └── DeepSearch.Tests/          # בדיקות יחידה (xUnit)
├── frontend/                      # צד לקוח (Angular)
│   └── src/app/
│       ├── pages/                     # query-builder, nl-query, saved-queries
│       ├── shared/                    # results-view, bar-chart
│       ├── services/                  # api.service
│       └── models/                    # ממשקי TypeScript
├── database/
│   ├── 01_schema.sql                  # סכמת PostgreSQL (לפריסה ל-GCP)
│   └── 02_seed.sql                    # נתוני דוגמה
├── docker-compose.yml             # PostgreSQL מקומי (אופציונלי)
├── docs/
│   ├── ARCHITECTURE.md                # תיעוד ארכיטקטורה מפורט
│   └── DEVOPS.md                      # סביבות, CI/CD, Secrets, פריסה ל-GCP
└── README.md
```

---

## הוראות הרצה

### דרישות מקדימות
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/) ו־Angular CLI (`npm install -g @angular/cli`)

### 1. צד שרת
```bash
cd backend
dotnet run --project src/DeepSearch.Api --urls http://localhost:5080
```
בהרצה הראשונה נוצר אוטומטית קובץ `deepsearch.db` (SQLite) ונזרעות ~5000 רשומות דוגמה.
- ה־API: `http://localhost:5080`
- תיעוד Swagger אינטראקטיבי: `http://localhost:5080/swagger`

### 2. צד לקוח
```bash
cd frontend
npm install
npm start
```
- האפליקציה: `http://localhost:4200`

### 3. בדיקות
```bash
cd backend
dotnet test
```

---

## סקירת ארכיטקטורה

המערכת בנויה בארכיטקטורת שכבות. כל שכבה תלויה רק בשכבה שמתחתיה, וה**ליבה (Core) אינה תלויה בכלום** — היא מגדירה ממשקים, וה־Infrastructure מספק מימוש (Dependency Inversion).

```
┌──────────────────────────────────────────────┐
│              Angular SPA (דפדפן)              │
│   בונה שאילתות · שאלה חופשית · שמורות         │
└───────────────────────┬──────────────────────┘
                        │ REST / JSON
┌───────────────────────▼──────────────────────┐
│            DeepSearch.Api (Controllers)       │  ← שכבת מצגת
├──────────────────────────────────────────────┤
│         DeepSearch.Core (Services)            │  ← לוגיקה עסקית + ממשקים
├──────────────────────────────────────────────┤
│   DeepSearch.Infrastructure (EF Core, NLP)    │  ← מימוש: DB, Repositories, LLM
└───────────────────────┬──────────────────────┘
                        │ SQL פרמטרי
┌───────────────────────▼──────────────────────┐
│        SQLite (dev) / PostgreSQL (prod)       │
└──────────────────────────────────────────────┘
```

**הרעיון המרכזי — `QueryDefinition`:** גם בונה השאילתות (תפריטים) וגם רכיב ה־LLM מייצרים את אותו אובייקט `QueryDefinition`, וממנו ההרצה זהה לחלוטין. ה־LLM הוא "מתרגם" שעומד בכניסה בלבד.

הסבר מלא עם דיאגרמות זרימה: ראו [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

---

## חיבור ל־LLM אמיתי (Gemini)

המערכת תומכת בשני מימושים של פירוש שפה חופשית, **מאחורי אותו ממשק `INlQueryParser`**:
- `RuleBasedNlQueryParser` — מבוסס חוקים (ברירת מחדל, ללא תלות חיצונית).
- `GeminiNlQueryParser` — Google Gemini אמיתי, עם **נפילה חזרה (fallback)** לפרסר החוקים אם ה־API נכשל.

המעבר נעשה דרך config בלבד — שום שורת קוד אחרת לא משתנה:

```bash
cd backend/src/DeepSearch.Api
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "<your-key-from-aistudio.google.com>"
dotnet user-secrets set "Nlp:Provider" "Gemini"
```
מפתח חינמי: [Google AI Studio](https://aistudio.google.com/app/apikey). המפתח נשמר ב־user-secrets ולא נכנס לקוד או ל־Git.

---

## בדיקות

פרויקט `DeepSearch.Tests` (xUnit) מכסה את הלוגיקה הרגישה ביותר:
- **QueryPhraseBuilder** — ניסוח נכון של כל סוגי המדדים.
- **RuleBasedNlQueryParser** — זיהוי פרמטרים, טיפול במקרי קצה (למשל "אנשים" לא מזוהה בטעות כ"נשים"), זיהוי שטויות, ומונחים לא נתמכים.

```bash
cd backend && dotnet test     # 11 בדיקות
```

---

## הנחות עבודה ומגבלות

- **נתוני דוגמה אקראיים** — ~5000 רשומות שנוצרות עם seed קבוע (תוצאות עקביות בין הרצות). אינם נתוני אמת.
- **SQLite בפיתוח** — נבחר לאפס־תשתית. בפרודקשן PostgreSQL; השאילתות (אגרגציות) זהות בשני הספקים. `MonthlyIncome` מיוצג כ־`double` (ולא `decimal`) לתאימות אגרגציות מול SQLite.
- **`EnsureCreated`** ליצירת הסכמה ב־PoC. בפרודקשן מומלץ EF Core Migrations.
- **הפרסר מבוסס החוקים תומך בעברית בלבד**; קלט באנגלית מקבל הודעה מתאימה. ה־LLM האמיתי (Gemini) מסיר מגבלה זו.
- **המגזרים בנתונים**: כללי / חרדי / ערבי בלבד. מונחים אחרים (דתי, יהודי וכו') מקבלים אזהרה שקופה.
- **`MetricField`** קיים במודל לצורך הרחבה עתידית, אך כרגע האגרגציות מתבצעות על `MonthlyIncome`.

---

## מסמכים נוספים
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — ארכיטקטורה מפורטת, זרימות מידע, החלטות תכנון.
- [docs/DEVOPS.md](docs/DEVOPS.md) — סביבות, CI/CD, ניהול קונפיגורציות ו־Secrets, פריסה ל־GCP.
