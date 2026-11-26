using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ei_back.Infrastructure.Context.Map;

public sealed class SupportTicketMap() : BaseMap<SupportTicket>("support_tickets")
{
    public override void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Id).HasDefaultValueSql(null).ValueGeneratedNever();
        builder.Property(x => x.OwnerId).HasColumnName("owner_id");
        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(SupportTicket.SubjectMaxLength);
        builder.Property(x => x.Type).HasColumnName("type");
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.Rating).HasColumnName("rating");
        builder.Property(x => x.Locale).HasColumnName("locale").HasMaxLength(5);
        builder.ToTable("support_tickets", table =>
            table.HasCheckConstraint("ck_support_tickets_rating", "rating IS NULL OR rating BETWEEN 1 AND 5"));
        builder.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Messages).WithOne().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.OwnerId, x.UpdatedAt, x.Id });
        builder.HasIndex(x => new { x.UpdatedAt, x.Id });
    }
}

public sealed class SupportTicketMessageMap() : BaseMap<SupportTicketMessage>("support_ticket_messages")
{
    public override void Configure(EntityTypeBuilder<SupportTicketMessage> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Id).HasDefaultValueSql(null).ValueGeneratedNever();
        builder.Property(x => x.TicketId).HasColumnName("ticket_id");
        builder.Property(x => x.AuthorId).HasColumnName("author_id");
        builder.Property(x => x.Body).HasColumnName("body").HasMaxLength(SupportTicket.MessageMaxLength);
        builder.Property(x => x.IsFromTeam).HasColumnName("is_from_team");
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => new { x.TicketId, x.CreatedAt, x.Id });
    }
}
