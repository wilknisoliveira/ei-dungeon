using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.Utils;

namespace ei_back.Core.Application.UseCase.SupportTicket.Interfaces;

public interface IListAdministrativeSupportTicketsUseCase
{
    Task<PagedSearchDto<SupportTicketListItem>> Handler(AdministrativeSupportTicketQuery query,
        CancellationToken cancellationToken);
}
