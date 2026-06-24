using FluentValidation;

namespace Lloka.Application.Bookings.Commands.CreateBooking;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.GuestId).NotEmpty();
        RuleFor(x => x.CheckInDate).NotEmpty();
        RuleFor(x => x.CheckOutDate)
            .NotEmpty()
            .GreaterThan(x => x.CheckInDate)
                .WithMessage("La fecha de check-out debe ser posterior a la de check-in.");
        RuleFor(x => x.GuestCount).GreaterThan(0);
    }
}
