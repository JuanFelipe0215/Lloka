using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using Lloka.Domain.ValueObjects;
using MediatR;

namespace Lloka.Application.Properties.Commands.CreateProperty;

public class CreatePropertyCommandHandler(
    IUserRepository     userRepo,
    IPropertyRepository propertyRepo,
    IUnitOfWork         unitOfWork
) : IRequestHandler<CreatePropertyCommand, CreatePropertyResponse>
{
    public async Task<CreatePropertyResponse> Handle(CreatePropertyCommand request, CancellationToken ct)
    {
        var owner = await userRepo.GetByIdAsync(request.OwnerId, ct)
            ?? throw new NotFoundException(nameof(User), request.OwnerId);

        if (!owner.IsOwner)
            throw new ConflictException("Solo los propietarios pueden publicar inmuebles.");

        var address = new Address(
            request.Street,
            request.City,
            request.Department,
            request.Country,
            request.Latitude,
            request.Longitude,
            request.PostalCode);

        var property = Property.Create(
            request.OwnerId,
            request.Title,
            request.Description,
            address,
            request.PricePerNight,
            request.MaxGuests);

        await propertyRepo.AddAsync(property, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new CreatePropertyResponse(property.Id, property.Title, property.Address.City, property.PricePerNight);
    }
}
