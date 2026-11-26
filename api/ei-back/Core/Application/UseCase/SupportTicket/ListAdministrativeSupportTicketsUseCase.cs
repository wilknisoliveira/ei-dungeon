using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.UseCase.SupportTicket.Interfaces;
using ei_back.Core.Application.Utils;

namespace ei_back.Core.Application.UseCase.SupportTicket;

public sealed class ListAdministrativeSupportTicketsUseCase(ISupportTicketRepository repository)
    : IListAdministrativeSupportTicketsUseCase
{
    public Task<PagedSearchDto<SupportTicketListItem>> Handler(AdministrativeSupportTicketQuery query,
        CancellationToken cancellationToken) =>
        repository.ListAdministrativeAsync(query, SupportTicketDateRange.Parse(query.CreatedFrom, query.CreatedTo),
            SupportTicketDateRange.Parse(query.UpdatedFrom, query.UpdatedTo), cancellationToken);
}
