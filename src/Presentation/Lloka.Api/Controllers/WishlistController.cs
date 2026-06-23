using System.Security.Claims;
using Lloka.Application.Wishlist.Commands.AddToWishlist;
using Lloka.Application.Wishlist.Commands.RemoveFromWishlist;
using Lloka.Application.Wishlist.Queries.GetUserWishlist;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lloka.Api.Controllers;

[ApiController]
[Route("api/wishlist")]
public class WishlistController(ISender mediator) : ControllerBase
{
    private Guid? CurrentUserId =>
        User.Identity?.IsAuthenticated == true
            ? Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!)
            : null;

    private Guid? AnonymousSessionId
    {
        get
        {
            var value = Request.Headers["X-Session-Id"].FirstOrDefault();
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlist(CancellationToken ct)
    {
        var userId    = CurrentUserId;
        var sessionId = AnonymousSessionId;

        if (userId is null && sessionId is null)
            return Ok(Array.Empty<object>());

        var result = await mediator.Send(new GetUserWishlistQuery(userId, sessionId), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddToWishlist(
        [FromBody] AddToWishlistRequest body,
        CancellationToken ct)
    {
        var command = new AddToWishlistCommand(
            body.PropertyId,
            CurrentUserId,
            CurrentUserId is null ? AnonymousSessionId : null);

        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetWishlist), result);
    }

    [HttpDelete("{propertyId:guid}")]
    public async Task<IActionResult> RemoveFromWishlist(Guid propertyId, CancellationToken ct)
    {
        var command = new RemoveFromWishlistCommand(
            propertyId,
            CurrentUserId,
            CurrentUserId is null ? AnonymousSessionId : null);

        await mediator.Send(command, ct);
        return NoContent();
    }
}

public record AddToWishlistRequest(Guid PropertyId);
