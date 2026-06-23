using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Metadata;

namespace DeepSearch.Core.Services;

/// <summary>
/// מרכיב את ה-Metadata שבונה השאילתות צריך:
/// - ערים ומגזרים נטענים דינמית מה-DB.
/// - מדדים / מגדרים / שדות פילוח מוגדרים כאן (יציבים).
/// </summary>
public class MetadataService : IMetadataService
{
    private readonly IMetadataRepository _metadata;

    public MetadataService(IMetadataRepository metadata)
    {
        _metadata = metadata;
    }

    public async Task<MetadataDto> GetMetadataAsync(CancellationToken ct = default)
    {
        var cities = await _metadata.GetCitiesAsync(ct);
        var sectors = await _metadata.GetSectorsAsync(ct);
        var districts = await _metadata.GetDistrictsAsync(ct);

        return new MetadataDto
        {
            Cities    = cities.Select(c => new OptionDto(c.Id.ToString(), c.Name)).ToList(),
            Districts = districts.Select(d => new OptionDto(d.Id.ToString(), d.Name)).ToList(),
            Sectors   = sectors.Select(s => new OptionDto(s.Id.ToString(), s.Name)).ToList(),
            Genders = new()
            {
                new OptionDto("female", "נשים"),
                new OptionDto("male", "גברים")
            },
            Metrics = new()
            {
                new OptionDto("Average", "ממוצע שכר"),
                new OptionDto("Count", "כמות אנשים"),
                new OptionDto("Sum", "סך שכר"),
                new OptionDto("EmploymentRate", "שיעור תעסוקה (%)")
            },
            GroupByFields = new()
            {
                new OptionDto("Year", "שנה"),
                new OptionDto("Gender", "מגדר"),
                new OptionDto("City", "עיר"),
                new OptionDto("District", "מחוז"),
                new OptionDto("Sector", "מגזר"),
                new OptionDto("AgeGroup", "קבוצת גיל")
            }
        };
    }
}
