using System.Text.Json;
using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ei_back.Infrastructure.Context.Map;

internal sealed class SparkCycleMap : IEntityTypeConfiguration<SparkCycle>
{
    public void Configure(EntityTypeBuilder<SparkCycle> builder)
    {
        SparkMap.SetNames(builder, "spark_cycle");
        builder.HasIndex(cycle => cycle.StartsAt).IsUnique();
    }
}

internal sealed class SparkGrantMap : IEntityTypeConfiguration<SparkGrant>
{
    public void Configure(EntityTypeBuilder<SparkGrant> builder)
    {
        SparkMap.SetNames(builder, "spark_grant");
        builder.HasOne<SparkCycle>().WithMany().HasForeignKey(grant => grant.CycleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SparkQueueRequest>().WithOne().HasForeignKey<SparkGrant>(grant => grant.QueueRequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(grant => new { grant.UserId, grant.GrantedAt });
        builder.HasIndex(grant => grant.UserId).IsUnique().HasFilter("kind = 0");
        builder.Property(grant => grant.Receipts).HasColumnType("jsonb");
    }
}

internal sealed class SparkQueueRequestMap : IEntityTypeConfiguration<SparkQueueRequest>
{
    public void Configure(EntityTypeBuilder<SparkQueueRequest> builder)
    {
        SparkMap.SetNames(builder, "spark_queue_request");
        builder.HasIndex(request => request.UserId).IsUnique().HasFilter("state IN (0, 1)");
        builder.HasIndex(request => request.EntryOrder).IsUnique();
        builder.HasIndex(request => new { request.State, request.Kind, request.EntryOrder });
        builder.HasIndex(request => new { request.State, request.DecidedAt });
        builder.Property(request => request.CultureName).HasMaxLength(8);
    }
}

internal sealed class SparkSettingsMap : IEntityTypeConfiguration<SparkSettings>
{
    public void Configure(EntityTypeBuilder<SparkSettings> builder)
    {
        SparkMap.SetNames(builder, "spark_settings");
        builder.Property(settings => settings.Id).ValueGeneratedNever();
    }
}

internal sealed class SparkDailySnapshotMap : IEntityTypeConfiguration<SparkDailySnapshot>
{
    public void Configure(EntityTypeBuilder<SparkDailySnapshot> builder)
    {
        SparkMap.SetNames(builder, "spark_daily_snapshot");
        builder.HasOne<SparkCycle>().WithMany().HasForeignKey(snapshot => snapshot.CycleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(snapshot => new { snapshot.CycleId, snapshot.Date }).IsUnique();
    }
}

internal sealed class SparkCycleChangeMap : IEntityTypeConfiguration<SparkCycleChange>
{
    public void Configure(EntityTypeBuilder<SparkCycleChange> builder)
    {
        SparkMap.SetNames(builder, "spark_cycle_change");
        builder.HasOne<SparkCycle>().WithMany().HasForeignKey(change => change.CycleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(change => new { change.CycleId, change.ChangedAt });
    }
}

internal static class SparkMap
{
    public static void SetNames<T>(EntityTypeBuilder<T> builder, string tableName) where T : class
    {
        builder.ToTable(tableName);
        foreach (var property in builder.Metadata.GetProperties())
            property.SetColumnName(JsonNamingPolicy.SnakeCaseLower.ConvertName(property.Name));
    }
}
