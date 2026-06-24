using Lloka.Domain.Entities;
using Lloka.Domain.ValueObjects;

namespace Lloka.Application.Common.Interfaces;

public interface IBookingRepository : IRepository<Booking>
{
    // Capa 1 del anti double-booking — Application valida antes de insertar.
    // Capa 2 = constraint EXCLUDE tstzrange en Postgres (Infrastructure).
    Task<bool> HasOverlapAsync(Guid propertyId, StayPeriod stayPeriod, CancellationToken ct = default);

    // Determina si el usuario ya tiene al menos una reserva confirmada.
    // Usado para aplicar la validación KYC solo en la primera reserva.
    Task<bool> HasConfirmedBookingAsync(Guid guestId, CancellationToken ct = default);
}
