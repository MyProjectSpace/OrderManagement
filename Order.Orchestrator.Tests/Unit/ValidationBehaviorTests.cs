using FluentAssertions;
using FluentValidation;
using MediatR;
using Order.Orchestrator.Application.Behaviors;

namespace Order.Orchestrator.Tests.Unit;

public class ValidationBehaviorTests
{
    private record DummyRequest(string Name) : IRequest<string>;

    private class DummyValidator : AbstractValidator<DummyRequest>
    {
        public DummyValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    [Fact]
    public async Task InvalidRequest_Throws_ValidationException_BeforeHandlerRuns()
    {
        var behavior = new ValidationBehavior<DummyRequest, string>(new[] { new DummyValidator() });

        var handlerWasCalled = false;
        RequestHandlerDelegate<string> next = () =>
        {
            handlerWasCalled = true;
            return Task.FromResult("ok");
        };

        var act = async () => await behavior.Handle(new DummyRequest(""), next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        handlerWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ValidRequest_PassesThrough()
    {
        var behavior = new ValidationBehavior<DummyRequest, string>(new[] { new DummyValidator() });

        RequestHandlerDelegate<string> next = () => Task.FromResult("ok");

        var result = await behavior.Handle(new DummyRequest("alice"), next, CancellationToken.None);

        result.Should().Be("ok");
    }
}
