using System.Security.Claims;
using Lloka.Application.Properties.Commands.CreateProperty;
using Lloka.Application.Properties.Commands.UpdateProperty;
using Lloka.Application.Properties.Queries.GetPropertyById;
using Lloka.Application.Properties.Queries.SearchProperties;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lloka.Api.Controllers;

[ApiController]
[Route("api/properties")]
public class PropertiesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string?   city      = null,
        [FromQuery] int       minGuests = 1,
        [FromQuery] DateOnly? checkIn   = null,
        [FromQuery] DateOnly? checkOut  = null,
        [FromQuery] int       page      = 1,
        [FromQuery] int       pageSize  = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new SearchPropertiesQuery(city, minGuests, checkIn, checkOut, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{propertyId:guid}")]
    public async Task<IActionResult> GetById(Guid propertyId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPropertyByIdQuery(propertyId), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreatePropertyRequest body,
        CancellationToken ct)
    {
        var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        var command = new CreatePropertyCommand(
            ownerId,
            body.Title,
            body.Description,
            body.Street,
            body.City,
            body.Department,
            body.Country,
            body.Latitude,
            body.Longitude,
            body.PostalCode,
            body.PricePerNight,
            body.MaxGuests);

        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { propertyId = result.PropertyId }, result);
    }

    [HttpPut("{propertyId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid propertyId,
        [FromBody] UpdatePropertyRequest body,
        CancellationToken ct)
    {
        var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        await mediator.Send(new UpdatePropertyCommand(
            propertyId,
            ownerId,
            body.Title,
            body.Description,
            body.PricePerNight,
            body.MaxGuests), ct);

        return NoContent();
    }
}

public record CreatePropertyRequest(
    string  Title,
    string  Description,
    string  Street,
    string  City,
    string  Department,
    string  Country,
    decimal Latitude,
    decimal Longitude,
    string? PostalCode,
    decimal PricePerNight,
    int     MaxGuests);

public record UpdatePropertyRequest(
    string  Title,
    string  Description,
    decimal PricePerNight,
    int     MaxGuests);
