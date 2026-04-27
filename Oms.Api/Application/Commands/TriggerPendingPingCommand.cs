using MediatR;

namespace Oms.Api.Application.Commands;

public record TriggerPendingPingCommand(string? CorrelationId = null) : IRequest<TriggerPendingPingResult>;

public record TriggerPendingPingResult(string CorrelationId);
