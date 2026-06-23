using DeepSearch.Core.Abstractions;
using DeepSearch.Core.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace DeepSearch.Api.Controllers;

/// <summary>מחזיר את ה-Metadata שבונה השאילתות צריך (ערים, מגזרים, מדדים...).</summary>
[ApiController]
[Route("api/metadata")]
public class MetadataController : ControllerBase
{
    private readonly IMetadataService _metadata;

    public MetadataController(IMetadataService metadata) => _metadata = metadata;

    [HttpGet]
    public async Task<ActionResult<MetadataDto>> Get(CancellationToken ct)
        => Ok(await _metadata.GetMetadataAsync(ct));
}
