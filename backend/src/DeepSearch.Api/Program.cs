using System.Text.Json.Serialization;
using DeepSearch.Api.Middleware;
using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Services;
using DeepSearch.Infrastructure;
using DeepSearch.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ----- שירותים (Dependency Injection) -----

// Controllers + הגדרת JSON: enums כטקסט ("Average" ולא 0)
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Swagger - תיעוד וניסוי ה-API מתוך הדפדפן
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// שכבת ה-Infrastructure (DB + Repositories + Parser) - הכל מרוכז שם
builder.Services.AddInfrastructure(builder.Configuration);

// שירותי ה-Core (הלוגיקה העסקית). נרשמים כאן כדי שה-Core יישאר נקי מתלות ב-framework.
builder.Services.AddScoped<IQueryPhraseBuilder, QueryPhraseBuilder>();
builder.Services.AddScoped<IQueryService, QueryService>();
builder.Services.AddScoped<IMetadataService, MetadataService>();
builder.Services.AddScoped<ISavedQueryService, SavedQueryService>();

// CORS - מאפשר ל-Angular (שרץ על פורט אחר) לקרוא ל-API. הכתובות מגיעות מה-config.
const string FrontendCors = "frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(o => o.AddPolicy(FrontendCors, p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// ----- אתחול ה-DB וזריעת נתוני הדוגמה -----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DeepSearchDbContext>();
    await DbInitializer.InitializeAsync(db);
}

// ----- צינור הבקשות (Middleware Pipeline) -----
app.UseMiddleware<ErrorHandlingMiddleware>();   // טיפול בשגיאות - ראשון, שיעטוף הכל

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// הגשת ה-Angular (קבצים סטטיים) מאותו שירות - מאפשר לינק יחיד בפרודקשן ללא CORS.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors(FrontendCors);
app.MapControllers();

// כל נתיב שאינו /api מוחזר ל-index.html כדי ש-Angular Routing יעבוד.
app.MapFallbackToFile("index.html");

app.Run();
