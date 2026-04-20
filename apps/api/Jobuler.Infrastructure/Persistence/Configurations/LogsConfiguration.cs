using Jobuler.Domain.Logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobuler.Infrastructure.Persistence.Configurations;

public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.ToTable("system_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.SpaceId).HasColumnName("space_id");
        builder.Property(l => l.Severity).HasColumnName("severity");
        builder.Property(l => l.EventType).HasColumnName("event_type").IsRequired();
        builder.Property(l => l.Message).HasColumnName("message").IsRequired();
        builder.Property(l => l.DetailsJson).HasColumnName("details_json").HasColumnType("jsonb");
        builder.Property(l => l.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(l => l.CorrelationId).HasColumnName("correlation_id");
        builder.Property(l => l.IsSensitive).HasColumnName("is_sensitive");
        builder.Property(l => l.CreatedAt).HasColumnName("created_at");
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.SpaceId).HasColumnName("space_id");
        builder.Property(l => l.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(l => l.Action).HasColumnName("action").IsRequired();
        builder.Property(l => l.EntityType).HasColumnName("entity_type");
        builder.Property(l => l.EntityId).HasColumnName("entity_id");
        builder.Property(l => l.BeforeJson).HasColumnName("before_json").HasColumnType("jsonb");
        builder.Property(l => l.AfterJson).HasColumnName("after_json").HasColumnType("jsonb");
        builder.Property(l => l.IpAddress).HasColumnName("ip_address");
        builder.Property(l => l.CorrelationId).HasColumnName("correlation_id");
        builder.Property(l => l.CreatedAt).HasColumnName("created_at");
    }
}
