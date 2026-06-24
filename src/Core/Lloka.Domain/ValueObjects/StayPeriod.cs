using Lloka.Domain.Common;

namespace Lloka.Domain.ValueObjects;

public sealed record StayPeriod
{

    private static readonly TimeSpan ColombiaTzOffset = TimeSpan.FromHours(-5);
    private const int CheckInHourLocal  = 14; 
    private const int CheckOutHourLocal = 12; 

    public DateTimeOffset CheckIn  { get; } 
    public DateTimeOffset CheckOut { get; } 
    public int Nights              { get; }

    public StayPeriod(DateOnly checkInDate, DateOnly checkOutDate)
    {
        if (checkOutDate <= checkInDate)
            throw new DomainException(
                $"La fecha de check-out ({checkOutDate}) debe ser posterior a la de check-in ({checkInDate}).");

        CheckIn = new DateTimeOffset(
            checkInDate.Year, checkInDate.Month, checkInDate.Day,
            CheckInHourLocal, 0, 0, ColombiaTzOffset);

        CheckOut = new DateTimeOffset(
            checkOutDate.Year, checkOutDate.Month, checkOutDate.Day,
            CheckOutHourLocal, 0, 0, ColombiaTzOffset);

 
        Nights = checkOutDate.DayNumber - checkInDate.DayNumber;
    }


    private StayPeriod(DateTimeOffset checkIn, DateTimeOffset checkOut)
    {
        CheckIn  = checkIn.ToOffset(ColombiaTzOffset);
        CheckOut = checkOut.ToOffset(ColombiaTzOffset);
        Nights   = (int)(CheckOut.Date - CheckIn.Date).TotalDays;
    }

    public decimal CalculateTotalAmount(decimal pricePerNight)
    {
        if (pricePerNight < 0)
            throw new DomainException("El precio por noche no puede ser negativo.");

        return pricePerNight * Nights;
    }
}
