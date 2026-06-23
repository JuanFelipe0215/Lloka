using FluentAssertions;
using Lloka.Domain.Entities;
using Lloka.Domain.ValueObjects;
using Lloka.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Lloka.IntegrationTests.Persistence;

public class BookingExclusionConstraintTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private LlokaDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<LlokaDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new LlokaDbContext(options);
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<(Property property, User user)> SeedAsync()
    {
        var address  = new Address("Cra 7 #1-1", "Bogotá", "Cundinamarca", "Colombia", 4.71m, -74.07m);
        var property = Property.Create(Guid.NewGuid(), "Test Property", "Descripción de prueba", address, 100_000m, 4);
        var user     = User.Create("guest@test.com", "hash", "Ana", "López");

        _context.Properties.Add(property);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (property, user);
    }

    private static Booking ConfirmedBooking(Guid propertyId, Guid guestId, DateOnly checkIn, DateOnly checkOut)
    {
        var booking = Booking.Create(propertyId, guestId, new StayPeriod(checkIn, checkOut), 100_000m, 1);
        booking.Confirm();
        return booking;
    }

    // (a) Dos reservas confirmadas con fechas solapadas → Postgres rechaza con 23P01.
    [Fact]
    public async Task OverlappingBookings_ThrowsExclusionViolation()
    {
        var (property, user) = await SeedAsync();

        var booking1 = ConfirmedBooking(property.Id, user.Id,
            new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 5));
        _context.Bookings.Add(booking1);
        await _context.SaveChangesAsync();

        var booking2 = ConfirmedBooking(property.Id, user.Id,
            new DateOnly(2025, 9, 3), new DateOnly(2025, 9, 7));
        _context.Bookings.Add(booking2);

        var act = async () => await _context.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        ex.WithInnerException<PostgresException>()
          .Which.SqlState.Should().Be("23P01");
    }

    // (b) Dos reservas no solapadas para el mismo inmueble → ambas se insertan sin error.
    [Fact]
    public async Task NonOverlappingBookings_BothSucceed()
    {
        var (property, user) = await SeedAsync();

        // Sep 1-5 y Sep 5-10: checkout del primero == checkin del segundo (no hay solapamiento)
        var booking1 = ConfirmedBooking(property.Id, user.Id,
            new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 5));
        var booking2 = ConfirmedBooking(property.Id, user.Id,
            new DateOnly(2025, 9, 5), new DateOnly(2025, 9, 10));

        _context.Bookings.AddRange(booking1, booking2);

        var act = async () => await _context.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    // (c) Reserva cancelada no bloquea las mismas fechas → WHERE Status <> 'Cancelled' la excluye.
    [Fact]
    public async Task CancelledBooking_DoesNotBlockOverlappingDates()
    {
        var (property, user) = await SeedAsync();

        var cancelled = Booking.Create(property.Id, user.Id,
            new StayPeriod(new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 5)), 100_000m, 1);
        cancelled.Confirm();
        cancelled.Cancel();
        _context.Bookings.Add(cancelled);
        await _context.SaveChangesAsync();

        // Fechas solapadas con la reserva cancelada — el constraint no aplica a canceladas
        var booking2 = ConfirmedBooking(property.Id, user.Id,
            new DateOnly(2025, 9, 3), new DateOnly(2025, 9, 7));
        _context.Bookings.Add(booking2);

        var act = async () => await _context.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }
}
