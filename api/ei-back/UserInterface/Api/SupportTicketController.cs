using ei_back.Core.Application.UseCase.SupportTicket.Dtos;
using ei_back.Core.Application.UseCase.SupportTicket.Interfaces;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Application.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ei_back.UserInterface.Api;

[ApiController]
[Route("api/support-tickets")]
[Authorize(Policy = "Bearer")]
[EnableRateLimiting("Authenticated")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class SupportTicketController(IGetUserIdUseCase getUserId) : ControllerBase
{
    /// <summary>Returns the signed-in player's support history.</summary>
    /// <remarks>Results are paginated and sorted by last update, descending by default.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedSearchDto<SupportTicketListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedSearchDto<SupportTicketListItem>>> ListMine(
        [FromQuery] SupportTicketPageQuery query, [FromServices] IListMySupportTicketsUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.Handler(getUserId.Handler(User), query, cancellationToken));

    /// <summary>Creates a private conversation with the Dungeon team.</summary>
    /// <remarks>The initial message is included in the returned conversation.</remarks>
    [HttpPost]
    [ProducesResponseType(typeof(SupportTicketDetail), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketDetail>> Create([FromBody] CreateSupportTicketRequest request,
        [FromServices] ICreateSupportTicketUseCase useCase, CancellationToken cancellationToken)
    {
        var ticket = await useCase.Handler(getUserId.Handler(User), request, cancellationToken);
        return CreatedAtAction(nameof(GetMine), new { id = ticket.Id }, ticket);
    }

    /// <summary>Returns the signed-in player's conversation and message history.</summary>
    /// <remarks>Returns 404 if the ticket does not belong to the caller.</remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SupportTicketDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketDetail>> GetMine(Guid id,
        [FromServices] IGetMySupportTicketUseCase useCase, CancellationToken cancellationToken) =>
        Ok(await useCase.Handler(getUserId.Handler(User), id, cancellationToken));

    /// <summary>Adds a player reply to an existing conversation.</summary>
    /// <remarks>Only the owner can reply. A closed conversation returns 409.</remarks>
    [HttpPost("{id:guid}/replies")]
    [ProducesResponseType(typeof(SupportTicketDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupportTicketDetail>> Reply(Guid id, [FromBody] ReplyToSupportTicketRequest request,
        [FromServices] IReplyToSupportTicketUseCase useCase, CancellationToken cancellationToken) =>
        Ok(await useCase.Handler(getUserId.Handler(User), id, request, cancellationToken));

    /// <summary>Records the owner's rating of a closed conversation.</summary>
    /// <remarks>Each conversation accepts one rating between one and five stars.</remarks>
    [HttpPost("{id:guid}/rating")]
    [ProducesResponseType(typeof(SupportTicketDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupportTicketDetail>> Rate(Guid id, [FromBody] RateSupportTicketRequest request,
        [FromServices] IRateSupportTicketUseCase useCase, CancellationToken cancellationToken) =>
        Ok(await useCase.Handler(getUserId.Handler(User), id, request, cancellationToken));

    /// <summary>Returns the administrative support queue.</summary>
    /// <remarks>
    /// Admin only. Filters and sorting apply before pagination.
    /// Date-only ranges include both UTC calendar dates. Timestamp bounds include the supplied instants.
    /// </remarks>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedSearchDto<SupportTicketListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedSearchDto<SupportTicketListItem>>> ListAdministrative(
        [FromQuery] AdministrativeSupportTicketQuery query, [FromServices] IListAdministrativeSupportTicketsUseCase useCase,
        CancellationToken cancellationToken) => Ok(await useCase.Handler(query, cancellationToken));

    /// <summary>Returns a conversation with its owner and message history.</summary>
    /// <remarks>Admin only.</remarks>
    [HttpGet("admin/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SupportTicketDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketDetail>> GetAdministrative(Guid id,
        [FromServices] IGetAdministrativeSupportTicketUseCase useCase, CancellationToken cancellationToken) =>
        Ok(await useCase.Handler(id, cancellationToken));

    /// <summary>Updates the type, status, or replies in a support conversation.</summary>
    /// <remarks>Admin only. A reply and status change in one request are committed together.</remarks>
    [HttpPatch("admin/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SupportTicketDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketDetail>> UpdateAdministrative(Guid id,
        [FromBody] UpdateSupportTicketRequest request, [FromServices] IUpdateAdministrativeSupportTicketUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.Handler(getUserId.Handler(User), id, request, cancellationToken));
}
