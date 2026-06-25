using System.Security.Claims;
using Lloka.Application.Kyc.Commands.SubmitKycDocument;
using Lloka.Application.Kyc.Queries.GetKycStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lloka.Api.Controllers;

[ApiController]
[Route("api/kyc")]
[Authorize]
public class KycController(ISender mediator) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var result = await mediator.Send(new GetKycStatusQuery(userId), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitKycRequest body,
        CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var result = await mediator.Send(new SubmitKycDocumentCommand(userId, body.DocumentBase64), ct);
        return Ok(result);
    }
}

public record SubmitKycRequest(string DocumentBase64);
