using EventBooking.Application.Abstractions;
using EventBooking.Application.Services;
using EventBooking.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EventBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateEventRequestValidator>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        return services;
    }
}
