# DevOps – Deep Search

מסמך זה מתאר את הסביבות המוצעות, תהליך ה־CI/CD, ניהול קונפיגורציות ו־Secrets, ופריסה ל־GCP.

> **הערה:** אין חובה לממש את התהליך בפועל — זהו תיאור ההמלצה. הפרויקט נבנה "ידידותי לפריסה" (config דרך environment variables, הפרדת ספקי DB/LLM).

---

## 1. סביבות

| סביבה | מטרה | DB | LLM |
|-------|------|----|----|
| **DEV** | פיתוח מקומי | SQLite (קובץ) | RuleBased (או Gemini עם מפתח אישי) |
| **TEST** | בדיקות / QA | Cloud SQL (PostgreSQL) – instance קטן | Gemini |
| **PROD** | ייצור | Cloud SQL (PostgreSQL) – עם גיבויים | Gemini |

ההבדל בין הסביבות הוא **קונפיגורציה בלבד** — לא קוד. אותו artifact נפרס לכל הסביבות.

---

## 2. ניהול קונפיגורציות

הקונפיגורציה נטענת לפי סדר עדיפויות (ASP.NET Core Configuration):
1. `appsettings.json` — ברירות מחדל.
2. `appsettings.{Environment}.json` — דריסה פר־סביבה.
3. **Environment Variables** — דריסה בפרודקשן (למשל `ConnectionStrings__Default`, `Nlp__Provider`).
4. **User Secrets** — בפיתוח מקומי בלבד (מפתחות API).

ערכים מרכזיים:
```
Database:Provider        → "Sqlite" | "Postgres"
ConnectionStrings:Default → connection string של ה-DB
Nlp:Provider             → "RuleBased" | "Gemini"
Cors:AllowedOrigins      → דומיין ה-Frontend
```

ב־Frontend: בפריסה הנוכחית ה־API וה־Angular מוגשים מאותו שירות (origin זהה), ולכן `ApiService` מזהה את כתובת ה־API בזמן ריצה — מקומית `http://localhost:5080/api` (כשרצים על פורט 4200), ובפרודקשן `/api` באותו origin. אין צורך ב־CORS בפרודקשן.

---

## 3. ניהול Secrets

**עיקרון: סודות לעולם לא בקוד ולא ב־Git.**

| סביבה | אחסון Secrets |
|-------|---------------|
| DEV | `dotnet user-secrets` (מאוחסן מחוץ לעץ הפרויקט) |
| TEST / PROD | **GCP Secret Manager** — נטען ל־Cloud Run בזמן ריצה כ־environment variable |

דוגמה (פיתוח):
```bash
dotnet user-secrets set "Gemini:ApiKey" "<key>"
```

בפרודקשן, ה־`Gemini:ApiKey` ו־connection string של ה־DB נשמרים ב־Secret Manager, וה־Cloud Run מקבל אליהם הרשאה דרך Service Account ייעודי.

---

## 4. תהליך CI/CD מוצע

```
   git push
      │
      ▼
┌─────────────┐   ┌──────────────┐   ┌─────────────────┐   ┌──────────────┐
│   Build     │──►│     Test     │──►│   Containerize  │──►│    Deploy    │
│ dotnet build│   │ dotnet test  │   │  docker build   │   │  Cloud Run   │
│ ng build    │   │ (+ng test)   │   │  push → Artifact│   │  (TEST→PROD) │
└─────────────┘   └──────────────┘   │     Registry    │   └──────────────┘
                                      └─────────────────┘
```

- **כלי**: GitHub Actions או Google Cloud Build.
- **Build**: קומפילציה של ה־API וה־Angular.
- **Test**: הרצת `dotnet test` (ובאופן אופציונלי `ng test`). כשלון עוצר את ה־pipeline.
- **Containerize**: בניית Docker image ל־API ודחיפה ל־Artifact Registry.
- **Deploy**: פריסה ל־Cloud Run — קודם ל־TEST, ולאחר אישור ל־PROD.

---

## 5. פריסה ל־GCP

**הפריסה בפועל (זו שרצה):** שירות **Cloud Run יחיד** — ה־API של .NET מגיש גם את קבצי ה־Angular הסטטיים (מ־`wwwroot`, עם fallback ל־`index.html`). כך מתקבל **לינק אחד, ללא CORS**. ה־Dockerfile בשורש בונה את ה־Angular ומטמיע אותו ב־API.

```
┌─────────────────────────────────┐     ┌────────────────────┐
│        Cloud Run (יחיד)         │     │  Cloud SQL (PG)    │
│  .NET API  +  Angular (wwwroot) │────►│   נתונים + שמורות   │
└────────────────┬────────────────┘     └────────────────────┘
                 │ קורא Secrets
                 ▼
        ┌──────────────────┐
        │  Secret Manager  │
        └──────────────────┘
```

| רכיב | שירות GCP | הערה |
|------|-----------|------|
| API + Frontend | **Cloud Run** | שירות אחד; ה־API מגיש את ה־Angular. scale-to-zero, חיוב לפי שימוש. |
| בסיס נתונים | **Cloud SQL for PostgreSQL** | מנוהל — גיבויים, אבטחה, עדכונים. (ב־PoC הנוכחי: SQLite ב־`/tmp`.) |
| Secrets | **Secret Manager** | מפתח Gemini + connection string. |
| חיבור API↔DB | **Cloud SQL Connector** | חיבור מאובטח בלי לחשוף את ה־DB לאינטרנט. |

**שלבי פריסה (בפועל):**
1. (לפרודקשן) יצירת instance של Cloud SQL והרצת `database/01_schema.sql` + `02_seed.sql`.
2. `gcloud run deploy --source .` — Cloud Build בונה את ה־Dockerfile (Angular + API) ופורס.
3. הגדרת env vars: `Gemini__ApiKey`, `Nlp__Provider`, ובפרודקשן גם connection string ו־`Database__Provider=Postgres` (מ־Secret Manager).

> **הפרדה אופציונלית:** ניתן גם להפריד את ה־Frontend ל־**Firebase Hosting** (CDN ייעודי) ולהשאיר את ה־API לבד ב־Cloud Run — אז יש להגדיר `Cors:AllowedOrigins` לכתובת ה־Frontend. בחרנו בשירות אחד לפשטות ולחיסכון ב־CORS.

> בזכות EF Core, המעבר ממקומי ל־GCP הוא בעיקר **החלפת connection string וספק** — הקוד עצמו אינו משתנה.

---

## 6. Dockerfile ל־API (לדוגמה)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/DeepSearch.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DeepSearch.Api.dll"]
```
(Cloud Run מצפה לפורט 8080 כברירת מחדל.)
