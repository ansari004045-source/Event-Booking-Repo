using EventBooking.Application.Dtos;
using FluentValidation;

namespace EventBooking.Application.Validation;

public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        RuleFor(x => x.StartsAt)
            .Must(startsAt => startsAt > DateTimeOffset.UtcNow)
            .WithMessage("StartsAt must be in the future.");

        RuleFor(x => x.SeatLimit)
            .InclusiveBetween(1, 100_000);
    }
}
