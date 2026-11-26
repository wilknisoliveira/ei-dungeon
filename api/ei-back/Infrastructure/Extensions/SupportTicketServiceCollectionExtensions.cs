using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.SupportTicket;
using ei_back.Core.Application.UseCase.SupportTicket.Interfaces;
using ei_back.Infrastructure.Context.Repository;

namespace ei_back.Infrastructure.Extensions;

internal static class SupportTicketServiceCollectionExtensions
{
    public static void AddSupportTickets(this IServiceCollection services)
    {
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<ICreateSupportTicketUseCase, CreateSupportTicketUseCase>();
        services.AddScoped<IListMySupportTicketsUseCase, ListMySupportTicketsUseCase>();
        services.AddScoped<IGetMySupportTicketUseCase, GetMySupportTicketUseCase>();
        services.AddScoped<IReplyToSupportTicketUseCase, ReplyToSupportTicketUseCase>();
        services.AddScoped<IRateSupportTicketUseCase, RateSupportTicketUseCase>();
        services.AddScoped<IListAdministrativeSupportTicketsUseCase, ListAdministrativeSupportTicketsUseCase>();
        services.AddScoped<IGetAdministrativeSupportTicketUseCase, GetAdministrativeSupportTicketUseCase>();
        services.AddScoped<IUpdateAdministrativeSupportTicketUseCase, UpdateAdministrativeSupportTicketUseCase>();
    }
}
