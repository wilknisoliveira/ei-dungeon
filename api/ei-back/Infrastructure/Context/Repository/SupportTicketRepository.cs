using System.Linq.Expressions;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.Utils;
using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace ei_back.Infrastructure.Context.Repository;

public sealed class SupportTicketRepository(EIContext context) : ISupportTicketRepository
{
    public void Add(SupportTicket ticket) => context.SupportTickets.Add(ticket);

    public Task<SupportTicket?> GetAsync(Guid ticketId, Guid? ownerId, CancellationToken cancellationToken) =>
        Details(context.SupportTickets.AsNoTracking(), ticketId, ownerId, includeOwner: ownerId == null)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<SupportTicket?> GetForUpdateAsync(Guid ticketId, Guid? ownerId, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction == null)
            throw new InvalidOperationException("A support ticket write requires an active transaction.");

        // Lock before reading state so replies, status changes and ratings use the current ticket.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM support_tickets WHERE id = {ticketId} AND ({ownerId}::uuid IS NULL OR owner_id = {ownerId}) FOR UPDATE",
            cancellationToken);

        return await Details(context.SupportTickets, ticketId, ownerId, includeOwner: true)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<PagedSearchDto<SupportTicketListItem>> ListMineAsync(Guid ownerId, SupportTicketPageQuery query,
        CancellationToken cancellationToken) =>
        PageAsync(context.SupportTickets.AsNoTracking().Where(ticket => ticket.OwnerId == ownerId), query,
            "updatedAt", false, cancellationToken);

    public Task<PagedSearchDto<SupportTicketListItem>> ListAdministrativeAsync(AdministrativeSupportTicketQuery query,
        SupportTicketDateRange createdRange, SupportTicketDateRange updatedRange, CancellationToken cancellationToken)
    {
        var tickets = context.SupportTickets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.UserEmail))
        {
            var email = EmailNormalizer.Normalize(query.UserEmail);
            tickets = tickets.Where(ticket => ticket.Owner.NormalizedEmail.Contains(email));
        }

        if (query.Type is { } type)
            tickets = tickets.Where(ticket => ticket.Type == type);
        if (query.Status is { } status)
            tickets = tickets.Where(ticket => ticket.Status == status);
        if (string.Equals(query.Rating, "unrated", StringComparison.OrdinalIgnoreCase))
            tickets = tickets.Where(ticket => ticket.Rating == null);
        else if (int.TryParse(query.Rating, out var rating))
            tickets = tickets.Where(ticket => ticket.Rating == rating);

        if (createdRange.From is { } createdFrom)
            tickets = tickets.Where(ticket => ticket.CreatedAt >= createdFrom);
        if (createdRange.To is { } createdTo)
            tickets = createdRange.EndsBefore
                ? tickets.Where(ticket => ticket.CreatedAt < createdTo)
                : tickets.Where(ticket => ticket.CreatedAt <= createdTo);
        if (updatedRange.From is { } updatedFrom)
            tickets = tickets.Where(ticket => ticket.UpdatedAt >= updatedFrom);
        if (updatedRange.To is { } updatedTo)
            tickets = updatedRange.EndsBefore
                ? tickets.Where(ticket => ticket.UpdatedAt < updatedTo)
                : tickets.Where(ticket => ticket.UpdatedAt <= updatedTo);

        return PageAsync(tickets, query, query.SortBy, true, cancellationToken);
    }

    private static IQueryable<SupportTicket> Details(IQueryable<SupportTicket> tickets, Guid ticketId, Guid? ownerId,
        bool includeOwner)
    {
        tickets = tickets.Where(ticket => ticket.Id == ticketId);
        if (ownerId is { } owner)
            tickets = tickets.Where(ticket => ticket.OwnerId == owner);
        if (includeOwner)
            tickets = tickets.Include(ticket => ticket.Owner);

        return tickets.Include(ticket => ticket.Messages);
    }

    private static async Task<PagedSearchDto<SupportTicketListItem>> PageAsync(IQueryable<SupportTicket> tickets,
        SupportTicketPageQuery query, string sortBy, bool includeOwner, CancellationToken cancellationToken)
    {
        var count = await tickets.CountAsync(cancellationToken);
        var offset = ((long)query.Page - 1) * query.PageSize;
        var ascending = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        List<SupportTicketListItem> items = offset >= count
            ? []
            : await Sort(tickets, sortBy, ascending).Skip((int)offset).Take(query.PageSize)
                .Select(ticket => new SupportTicketListItem(ticket.Id, ticket.Subject, ticket.Type, ticket.Status,
                    ticket.CreatedAt, ticket.UpdatedAt, ticket.Rating, includeOwner ? ticket.Owner.Email : null))
                .ToListAsync(cancellationToken);

        return new PagedSearchDto<SupportTicketListItem>
        {
            CurrentPage = query.Page,
            PageSize = query.PageSize,
            TotalResults = count,
            SortDirection = ascending ? "asc" : "desc",
            Items = items
        };
    }

    private static IOrderedQueryable<SupportTicket> Sort(IQueryable<SupportTicket> tickets, string sortBy, bool ascending)
    {
        IOrderedQueryable<SupportTicket> OrderBy<TKey>(Expression<Func<SupportTicket, TKey>> key) =>
            ascending ? tickets.OrderBy(key) : tickets.OrderByDescending(key);

        return (sortBy.ToLowerInvariant() switch
        {
            "user" => OrderBy(ticket => ticket.Owner.NormalizedEmail),
            "type" => OrderBy(ticket => ticket.Type),
            "status" => OrderBy(ticket => ticket.Status),
            "rating" => OrderBy(ticket => ticket.Rating),
            "createdat" => OrderBy(ticket => ticket.CreatedAt),
            _ => OrderBy(ticket => ticket.UpdatedAt)
        }).ThenBy(ticket => ticket.Id);
    }
}
