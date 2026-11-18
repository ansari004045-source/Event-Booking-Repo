using EventBooking.Application.Dtos;
using FluentValidation;

namespace EventBooking.Application.Validation;

public class BookSeatsRequestValidator : AbstractValidator<BookSeatsRequest>
{
    public BookSeatsRequestValidator()
    {
        RuleFor(x => x.AttendeeName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.AttendeeEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.Seats)
            .InclusiveBetween(1, 50);
    }
}
