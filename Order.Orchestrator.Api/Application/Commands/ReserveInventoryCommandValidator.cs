using FluentValidation;

namespace Order.Orchestrator.Application.Commands;

public class ReserveInventoryCommandValidator : AbstractValidator<ReserveInventoryCommand>
{
    public ReserveInventoryCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).NotEmpty();
    }
}
