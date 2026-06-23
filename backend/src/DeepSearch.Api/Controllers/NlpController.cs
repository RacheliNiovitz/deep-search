using DeepSearch.Api.Models;
using DeepSearch.Core.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DeepSearch.Api.Controllers;

/// <summary>
/// מסך השאלה החופשית (דרישה 5): מקבל טקסט, מפרש אותו ל-QueryDefinition
/// ומחזיר גם את הפירוש הקריא ("כך הבנתי את השאלה"). ההרצה עצמה דרך /api/queries/execute.
/// </summary>
[ApiController]
[Route("api/nlp")]
public class NlpController : ControllerBase
{
    private readonly INlQueryParser _parser;

    public NlpController(INlQueryParser parser) => _parser = parser;

    [HttpPost("parse")]
    public async Task<ActionResult<NlParseResult>> Parse([FromBody] NlQueryRequest request, CancellationToken ct)
        => Ok(await _parser.ParseAsync(request.Question, ct));
}
