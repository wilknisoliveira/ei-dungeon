using System.ComponentModel.DataAnnotations;
using ei_back.Core.Domain.Enums;
using Ticket = ei_back.Core.Domain.Entity.SupportTicket;

namespace ei_back.Core.Application.UseCase.SupportTicket.Dtos;

public sealed record CreateSupportTicketRequest(
    [Required, StringLength(Ticket.SubjectMaxLength)] string Subject,
    [Required, EnumDataType(typeof(SupportTicketType))] SupportTicketType? Type,
    [Required, StringLength(Ticket.MessageMaxLength)] string Message);

public sealed record ReplyToSupportTicketRequest([Required, StringLength(Ticket.MessageMaxLength)] string Message);

public sealed record RateSupportTicketRequest([Range(1, 5)] int Rating);

public sealed record UpdateSupportTicketRequest(
    [EnumDataType(typeof(SupportTicketType))] SupportTicketType? Type,
    [EnumDataType(typeof(SupportTicketStatus))] SupportTicketStatus? Status,
    [StringLength(Ticket.MessageMaxLength)] string? Message);

public sealed record SupportTicketListItem(Guid Id, string Subject, SupportTicketType Type, SupportTicketStatus Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, int? Rating, string? OwnerEmail);

public sealed record SupportTicketDetail(Guid Id, string Subject, SupportTicketType Type, SupportTicketStatus Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, int? Rating, string? OwnerEmail,
    IReadOnlyList<SupportTicketMessageDto> Messages);

public sealed record SupportTicketMessageDto(Guid Id, string Body, DateTimeOffset CreatedAt, bool IsFromTeam);
