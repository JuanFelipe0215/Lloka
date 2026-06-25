using System.Security.Claims;
using Lloka.Application.Bookings.Commands.CancelBooking;
using Lloka.Application.Bookings.Commands.CreateBooking;
using Lloka.Application.Bookings.Queries.GetUserBookings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lloka.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUserBookings(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var result = await mediator.Send(new GetUserBookingsQuery(userId), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking(
        [FromBody] CreateBookingRequest body,
        CancellationToken ct)
    {
        var guestId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        var command = new CreateBookingCommand(
            body.PropertyId,
            guestId,
            body.CheckInDate,
            body.CheckOutDate,
            body.GuestCount,
            body.AnonymousSessionId);

        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(CreateBooking), new { bookingId = result.BookingId }, result);
    }

    [HttpDelete("{bookingId:guid}")]
    public async Task<IActionResult> CancelBooking(Guid bookingId, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        await mediator.Send(new CancelBookingCommand(bookingId, userId), ct);
        return NoContent();
    }
}

public record CreateBookingRequest(
    Guid     PropertyId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int      GuestCount,
    Guid?    AnonymousSessionId = null);
