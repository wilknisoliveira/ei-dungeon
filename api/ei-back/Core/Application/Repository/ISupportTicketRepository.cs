using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.Utils;
using ei_back.Core.Domain.Entity;

namespace ei_back.Core.Application.Repository;

public interface ISupportTicketRepository
{
    void Add(SupportTicket ticket);

    Task<SupportTicket?> GetAsync(Guid ticketId, Guid? ownerId, CancellationToken cancellationToken);
    Task<SupportTicket?> GetForUpdateAsync(Guid ticketId, Guid? ownerId, CancellationToken cancellationToken);

    Task<PagedSearchDto<SupportTicketListItem>> ListMineAsync(Guid ownerId, SupportTicketPageQuery query,
        CancellationToken cancellationToken);

    Task<PagedSearchDto<SupportTicketListItem>> ListAdministrativeAsync(AdministrativeSupportTicketQuery query,
        SupportTicketDateRange createdRange, SupportTicketDateRange updatedRange, CancellationToken cancellationToken);
}
