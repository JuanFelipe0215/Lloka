using Lloka.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lloka.Infrastructure.Persistence;

public class LlokaDbContext(DbContextOptions<LlokaDbContext> options) : DbContext(options)
{
    public DbSet<User>            Users            => Set<User>();
    public DbSet<Property>        Properties       => Set<Property>();
    public DbSet<PropertyImage>   PropertyImages   => Set<PropertyImage>();
    public DbSet<Booking>         Bookings         => Set<Booking>();
    public DbSet<KycVerification> KycVerifications => Set<KycVerification>();
    public DbSet<WishlistItem>    WishlistItems    => Set<WishlistItem>();
    public DbSet<OutboxMessage>   OutboxMessages   => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LlokaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
