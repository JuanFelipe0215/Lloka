using System.Text.Json;
using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using Lloka.Domain.Enums;
using Lloka.Domain.ValueObjects;
using MediatR;

namespace Lloka.Application.Bookings.Commands.CreateBooking;

public class CreateBookingCommandHandler(
    IPropertyRepository       propertyRepo,
    IUserRepository           userRepo,
    IBookingRepository        bookingRepo,
    IRepository<OutboxMessage> outboxRepo,
    IUnitOfWork               unitOfWork
) : IRequestHandler<CreateBookingCommand, CreateBookingResponse>
{
    public async Task<CreateBookingResponse> Handle(CreateBookingCommand request, CancellationToken ct)
    {
        // 1. Cargar el inmueble
        var property = await propertyRepo.GetByIdAsync(request.PropertyId, ct)
            ?? throw new NotFoundException(nameof(Property), request.PropertyId);

        // 2. Verificar que el inmueble esté activo
        if (property.Status != PropertyStatus.Active)
            throw new ConflictException("El inmueble no está disponible para reservas.");

        // 3. El propietario no puede reservar su propio inmueble
        if (request.GuestId == property.OwnerId)
            throw new ConflictException("El propietario no puede reservar su propio inmueble.");

        // 4. Cargar el huésped
        var guest = await userRepo.GetByIdAsync(request.GuestId, ct)
            ?? throw new NotFoundException(nameof(User), request.GuestId);

        // 5. KYC obligatorio antes de la primera reserva (requisito del PDF)
        var isFirstBooking = !await bookingRepo.HasConfirmedBookingAsync(request.GuestId, ct);
        if (isFirstBooking && guest.KycStatus != KycStatus.Approved)
            throw new ConflictException(
                "Debes completar la verificación de identidad (KYC) antes de realizar tu primera reserva.");

        // 6. Construir el período de estadía — valida fechas y fija horas de negocio
        var stayPeriod = new StayPeriod(request.CheckInDate, request.CheckOutDate);

        // 7. Verificar capacidad máxima del inmueble
        if (request.GuestCount > property.MaxGuests)
            throw new ConflictException(
                $"El inmueble permite un máximo de {property.MaxGuests} huéspedes.");

        // 8. Verificar disponibilidad — Capa 1 anti double-booking
        //    Capa 2 = constraint EXCLUDE tstzrange en Postgres (Infrastructure)
        if (await bookingRepo.HasOverlapAsync(request.PropertyId, stayPeriod, ct))
            throw new ConflictException(
                "El inmueble no está disponible para las fechas seleccionadas.");

        // 9. Crear y confirmar la reserva (instant booking — MVP sin flujo de pago)
        var booking = Booking.Create(
            request.PropertyId,
            request.GuestId,
            stayPeriod,
            property.PricePerNight,
            request.GuestCount,
            request.AnonymousSessionId);

        booking.Confirm();

        // 10. Registrar evento en el Outbox — misma transacción que la reserva
        var payload = JsonSerializer.Serialize(new
        {
            bookingId     = booking.Id,
            guestId       = booking.GuestId,
            guestEmail    = guest.Email,
            guestName     = guest.FullName,
            propertyId    = booking.PropertyId,
            propertyTitle = property.Title,
            checkIn       = booking.StayPeriod.CheckIn,
            checkOut      = booking.StayPeriod.CheckOut,
            totalAmount   = booking.TotalAmount,
            nights        = booking.StayPeriod.Nights
        });

        var outboxMessage = OutboxMessage.Create("booking.confirmed", payload);

        // 11. Persistir todo en una sola transacción
        await bookingRepo.AddAsync(booking, ct);
        await outboxRepo.AddAsync(outboxMessage, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new CreateBookingResponse(
            booking.Id,
            booking.TotalAmount,
            booking.StayPeriod.CheckIn,
            booking.StayPeriod.CheckOut,
            booking.StayPeriod.Nights,
            booking.Status);
    }
}
