using FluentValidation;

namespace Payment.Gateway.Api.Application.Commands;

public class PublishPaymentConfirmedCommandValidator : AbstractValidator<PublishPaymentConfirmedCommand>
{
    public PublishPaymentConfirmedCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.CustomerId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).NotEmpty();
        RuleFor(x => x.Total).GreaterThan(0);
    }
}
