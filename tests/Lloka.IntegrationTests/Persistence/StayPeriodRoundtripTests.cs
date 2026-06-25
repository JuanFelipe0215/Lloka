using FluentAssertions;
using Lloka.Domain.Entities;
using Lloka.Domain.ValueObjects;
using Lloka.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Lloka.IntegrationTests.Persistence;

public class StayPeriodRoundtripTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private LlokaDbContext _context = null!;
    private string         _connectionString = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();

        var options = new DbContextOptionsBuilder<LlokaDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        _context = new LlokaDbContext(options);
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private LlokaDbContext FreshContext() =>
        new(new DbContextOptionsBuilder<LlokaDbContext>()
            .UseNpgsql(_connectionString)
            .Options);

    [Fact]
    public async Task Booking_AfterDbRoundtrip_StayPeriodHasColombiaOffset()
    {
        // Arrange: seed prop + user, create booking con StayPeriod conocido
        var address  = new Address("Cra 7 #1-1", "Bogotá", "Cundinamarca", "Colombia", 4.71m, -74.07m);
        var property = Property.Create(Guid.NewGuid(), "Casa Test", "Descripción", address, 100_000m, 4);
        var user     = User.Create("guest@test.com", "hash", "Ana", "López");

        _context.Properties.Add(property);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var checkIn  = new DateOnly(2025, 10, 1);
        var checkOut = new DateOnly(2025, 10, 5);
        var booking  = Booking.Create(property.Id, user.Id, new StayPeriod(checkIn, checkOut), 100_000m, 2);
        booking.Confirm();

        // Verificar valores ANTES de persistir (constructor público, offset Colombia -05:00)
        booking.StayPeriod.CheckIn.Offset.Should().Be(TimeSpan.FromHours(-5),
            "el constructor público crea con offset Colombia");
        booking.StayPeriod.CheckIn.Hour.Should().Be(14);
        booking.StayPeriod.CheckOut.Hour.Should().Be(12);

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        var bookingId = booking.Id;

        // Act: leer con un DbContext NUEVO para forzar materialización real desde SQL
        // (misma conexión = identity cache devolvería la instancia en memoria)
        await using var freshContext = FreshContext();
        var retrieved = await freshContext.Bookings.FindAsync(bookingId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.StayPeriod.CheckIn.Offset.Should().Be(TimeSpan.FromHours(-5),
            "el constructor privado de StayPeriod aplica ToOffset(-5) al materializar desde DB");
        retrieved.StayPeriod.CheckOut.Offset.Should().Be(TimeSpan.FromHours(-5));
        retrieved.StayPeriod.CheckIn.Hour.Should().Be(14,
            "14h Colombia debe preservarse tras el roundtrip UTC → Colombia");
        retrieved.StayPeriod.CheckOut.Hour.Should().Be(12);
        retrieved.StayPeriod.Nights.Should().Be(4);
    }
}
