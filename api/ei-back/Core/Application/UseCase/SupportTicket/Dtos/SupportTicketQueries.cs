using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.UseCase.SupportTicket.Dtos;

public class SupportTicketPageQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    [RegularExpression("(?i:asc|desc)")]
    public string SortDirection { get; init; } = "desc";
}

public sealed class AdministrativeSupportTicketQuery : SupportTicketPageQuery
{
    [StringLength(254)]
    public string? UserEmail { get; init; }

    [EnumDataType(typeof(SupportTicketType))]
    public SupportTicketType? Type { get; init; }

    [EnumDataType(typeof(SupportTicketStatus))]
    public SupportTicketStatus? Status { get; init; }

    [RegularExpression("(?i:unrated|[1-5])")]
    public string? Rating { get; init; }

    [RegularExpression("(?i:user|type|status|rating|createdAt|updatedAt)")]
    public string SortBy { get; init; } = "updatedAt";

    public string? CreatedFrom { get; init; }
    public string? CreatedTo { get; init; }
    public string? UpdatedFrom { get; init; }
    public string? UpdatedTo { get; init; }
}

public sealed record SupportTicketDateRange(DateTimeOffset? From, DateTimeOffset? To, bool EndsBefore)
{
    internal static SupportTicketDateRange Parse(string? from, string? to)
    {
        var start = ParseBound(from, false, out _);
        var end = ParseBound(to, true, out var endsBefore);
        if (start > end || (endsBefore && start == end))
            throw InvalidRange("The start date must be before or equal to the end date.");
        return new SupportTicketDateRange(start, end, endsBefore);
    }

    private static DateTimeOffset? ParseBound(string? value, bool upper, out bool exclusive)
    {
        exclusive = false;
        if (value == null)
            return null;

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            if (upper && date == DateOnly.MaxValue)
                return DateTimeOffset.MaxValue;

            exclusive = upper;
            return new DateTimeOffset((upper ? date.AddDays(1) : date).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        }

        string[] timestampFormats = ["yyyy-MM-dd'T'HH:mm:ssK", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK"];
        if (DateTimeOffset.TryParseExact(value, timestampFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                out var timestamp))
            return timestamp.ToUniversalTime();

        throw InvalidRange("Use ISO date or timestamp filters.");
    }

    private static SupportTicketException InvalidRange(string message) => new("support_ticket_invalid", message);
}
