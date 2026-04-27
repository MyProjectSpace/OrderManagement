using FluentValidation;

namespace Order.Orchestrator.Application.Commands;

public class EnqueuePendingSyncCommandValidator : AbstractValidator<EnqueuePendingSyncCommand>
{
    public EnqueuePendingSyncCommandValidator()
    {
        RuleFor(x => x.CorrelationId).NotEmpty().MaximumLength(64);
    }
}
