using System.Text.Json;
using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using MediatR;

namespace Lloka.Application.Bookings.Commands.CancelBooking;

public class CancelBookingCommandHandler(
    IBookingRepository         bookingRepo,
    IRepository<OutboxMessage> outboxRepo,
    IUnitOfWork                unitOfWork
) : IRequestHandler<CancelBookingCommand, Unit>
{
    public async Task<Unit> Handle(CancelBookingCommand request, CancellationToken ct)
    {
        var booking = await bookingRepo.GetByIdAsync(request.BookingId, ct)
            ?? throw new NotFoundException(nameof(Booking), request.BookingId);

        if (booking.GuestId != request.RequestingUserId)
            throw new ConflictException("No tienes permisos para cancelar esta reserva.");

        booking.Cancel();

        var payload = JsonSerializer.Serialize(new
        {
            bookingId  = booking.Id,
            guestId    = booking.GuestId,
            propertyId = booking.PropertyId
        });

        var outboxMessage = OutboxMessage.Create("booking.cancelled", payload);

        bookingRepo.Update(booking);
        await outboxRepo.AddAsync(outboxMessage, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
