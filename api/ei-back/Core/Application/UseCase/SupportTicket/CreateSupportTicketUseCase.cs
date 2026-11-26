using System.Globalization;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.UseCase.SupportTicket.Interfaces;
using ei_back.Core.Domain.DomainExceptions;
using Ticket = ei_back.Core.Domain.Entity.SupportTicket;

namespace ei_back.Core.Application.UseCase.SupportTicket;

public sealed class CreateSupportTicketUseCase(ISupportTicketRepository repository, IUserRepository users,
    IUnitOfWork unitOfWork, ISupportTicketNotificationService notifications, TimeProvider clock) : ICreateSupportTicketUseCase
{
    public async Task<SupportTicketDetail> Handler(Guid ownerId, CreateSupportTicketRequest request,
        CancellationToken cancellationToken)
    {
        var type = request.Type ?? throw new SupportTicketException("support_ticket_invalid", "A conversation type is required.");
        var owner = await users.GetUserAndRolesAsync(ownerId, cancellationToken)
            ?? throw new SupportTicketException("support_ticket_not_found", "Account not found.");

        var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
        {
            "pt" => "pt-BR",
            "es" => "es",
            _ => "en"
        };

        var ticket = new Ticket(ownerId, request.Subject, type, request.Message, locale, clock.GetUtcNow());
        repository.Add(ticket);
        await unitOfWork.CommitAsync(cancellationToken);
        await notifications.NotifyTeamAsync(new(ticket.Id, ticket.Subject, ticket.Type, owner.Email, true, ticket.UpdatedAt),
            cancellationToken);

        return SupportTicketDetails.Map(ticket);
    }
}
