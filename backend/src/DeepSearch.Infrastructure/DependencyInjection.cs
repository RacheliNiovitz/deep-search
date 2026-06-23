using DeepSearch.Core.Abstractions;
using DeepSearch.Infrastructure.Data;
using DeepSearch.Infrastructure.Nlp;
using DeepSearch.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DeepSearch.Infrastructure;

/// <summary>
/// רישום כל שירותי ה-Infrastructure ב-DI. מרוכז כאן כדי שה-Program.cs יישאר נקי.
/// בחירת ה-DB נעשית לפי הקונפיגורציה - מקומית SQLite, בפרודקשן PostgreSQL.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["Database:Provider"] ?? "Sqlite";
        var connectionString = config.GetConnectionString("Default") ?? "Data Source=deepsearch.db";

        services.AddDbContext<DeepSearchDbContext>(options =>
        {
            if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
                options.UseNpgsql(connectionString);     // פרודקשן - GCP Cloud SQL
            else
                options.UseSqlite(connectionString);     // פיתוח מקומי
        });

        // Repositories
        services.AddScoped<IPopulationRepository, PopulationRepository>();
        services.AddScoped<IMetadataRepository, MetadataRepository>();
        services.AddScoped<ISavedQueryRepository, SavedQueryRepository>();

        // ★ רכיב ה-LLM/Parser - נקודת ההחלפה ★
        // הפרסר מבוסס החוקים תמיד רשום (גם כברירת מחדל וגם כ-fallback ל-Gemini).
        services.AddScoped<RuleBasedNlQueryParser>();

        var nlpProvider = config["Nlp:Provider"] ?? "RuleBased";
        var geminiKey = config["Gemini:ApiKey"];

        if (nlpProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(geminiKey))
        {
            // LLM אמיתי - אותו ממשק בדיוק, רק מימוש אחר.
            services.AddHttpClient<GeminiNlQueryParser>();
            services.AddScoped<INlQueryParser>(sp => sp.GetRequiredService<GeminiNlQueryParser>());
        }
        else
        {
            services.AddScoped<INlQueryParser>(sp => sp.GetRequiredService<RuleBasedNlQueryParser>());
        }

        return services;
    }
}
