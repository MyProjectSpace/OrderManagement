using FluentValidation;

namespace Inventory.Service.Application.Commands;

public class ApplyInventoryCommandValidator : AbstractValidator<ApplyInventoryCommand>
{
    public ApplyInventoryCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).NotEmpty().MaximumLength(64);
    }
}
