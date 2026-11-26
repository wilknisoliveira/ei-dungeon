using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.UseCase.SupportTicket.Interfaces;
using ei_back.Core.Application.Utils;

namespace ei_back.Core.Application.UseCase.SupportTicket;

public sealed class ListMySupportTicketsUseCase(ISupportTicketRepository repository) : IListMySupportTicketsUseCase
{
    public Task<PagedSearchDto<SupportTicketListItem>> Handler(Guid ownerId, SupportTicketPageQuery query,
        CancellationToken cancellationToken) => repository.ListMineAsync(ownerId, query, cancellationToken);
}
