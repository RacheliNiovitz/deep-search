using DeepSearch.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeepSearch.Infrastructure.Data;

/// <summary>
/// יוצר את בסיס הנתונים (אם אינו קיים) וזורע נתוני דוגמה אם הוא ריק.
/// נקרא פעם אחת בעליית האפליקציה. משתמש ב-seed קבוע כדי שהנתונים יהיו זהים בכל הרצה.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(DeepSearchDbContext db)
    {
        // יוצר את הסכמה אם צריך (ל-PoC; בפרודקשן עדיף EF Migrations)
        await db.Database.EnsureCreatedAsync();

        // אם כבר יש נתונים - לא זורעים שוב
        if (await db.Cities.AnyAsync())
            return;

        // מחוזות
        var districts = new List<District>
        {
            new() { Name = "ירושלים" },
            new() { Name = "תל אביב" },
            new() { Name = "חיפה" },
            new() { Name = "הדרום" },
            new() { Name = "המרכז" },
            new() { Name = "הצפון" }
        };
        db.Districts.AddRange(districts);
        await db.SaveChangesAsync();

        District D(string name) => districts.First(d => d.Name == name);

        // ערים (2 לכל מחוז) - כדי שפילוח לפי מחוז יקבץ כמה ערים
        var cities = new List<City>
        {
            new() { Name = "ירושלים",   DistrictId = D("ירושלים").Id },
            new() { Name = "בית שמש",   DistrictId = D("ירושלים").Id },
            new() { Name = "תל אביב",   DistrictId = D("תל אביב").Id },
            new() { Name = "בני ברק",   DistrictId = D("תל אביב").Id },
            new() { Name = "חיפה",      DistrictId = D("חיפה").Id },
            new() { Name = "חדרה",      DistrictId = D("חיפה").Id },
            new() { Name = "באר שבע",   DistrictId = D("הדרום").Id },
            new() { Name = "אשדוד",     DistrictId = D("הדרום").Id },
            new() { Name = "פתח תקווה", DistrictId = D("המרכז").Id },
            new() { Name = "רחובות",    DistrictId = D("המרכז").Id },
            new() { Name = "נצרת",      DistrictId = D("הצפון").Id },
            new() { Name = "עכו",       DistrictId = D("הצפון").Id }
        };
        var sectors = new List<Sector>
        {
            new() { Name = "כללי" },
            new() { Name = "חרדי" },
            new() { Name = "ערבי" }
        };

        db.Cities.AddRange(cities);
        db.Sectors.AddRange(sectors);
        await db.SaveChangesAsync();   // כדי שיהיו להם Id-ים

        var random = new Random(42);   // seed קבוע = נתונים זהים בכל הרצה
        var records = new List<PopulationRecord>(5000);

        for (var i = 0; i < 5000; i++)
        {
            records.Add(new PopulationRecord
            {
                Gender        = random.NextDouble() < 0.5 ? "female" : "male",
                Age           = random.Next(20, 65),                    // 20..64
                CityId        = cities[random.Next(cities.Count)].Id,
                SectorId      = sectors[random.Next(sectors.Count)].Id,
                Year          = random.Next(2020, 2025),                // 2020..2024
                MonthlyIncome = Math.Round(4000 + random.NextDouble() * 16000, 2),
                IsEmployed    = random.NextDouble() < 0.75
            });
        }

        db.PopulationRecords.AddRange(records);
        await db.SaveChangesAsync();
    }
}
