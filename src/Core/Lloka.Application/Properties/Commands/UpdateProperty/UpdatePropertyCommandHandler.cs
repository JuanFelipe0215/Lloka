using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using MediatR;

namespace Lloka.Application.Properties.Commands.UpdateProperty;

public class UpdatePropertyCommandHandler(
    IPropertyRepository propertyRepo,
    IUnitOfWork         unitOfWork
) : IRequestHandler<UpdatePropertyCommand, Unit>
{
    public async Task<Unit> Handle(UpdatePropertyCommand request, CancellationToken ct)
    {
        var property = await propertyRepo.GetByIdAsync(request.PropertyId, ct)
            ?? throw new NotFoundException(nameof(Property), request.PropertyId);

        if (property.OwnerId != request.OwnerId)
            throw new ConflictException("No tienes permisos para modificar este inmueble.");

        property.Update(request.Title, request.Description, request.PricePerNight, request.MaxGuests);
        propertyRepo.Update(property);
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
