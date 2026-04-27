using MediatR;
using Shared.Contracts;

namespace Oms.Api.Application.Queries;

public record GetPendingOrdersQuery : IRequest<IReadOnlyList<PendingOrder>>;
