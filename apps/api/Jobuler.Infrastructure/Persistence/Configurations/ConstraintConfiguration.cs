using Jobuler.Domain.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobuler.Infrastructure.Persistence.Configurations;

public class ConstraintRuleConfiguration : IEntityTypeConfiguration<ConstraintRule>
{
    public void Configure(EntityTypeBuilder<ConstraintRule> builder)
    {
        builder.ToTable("constraint_rules");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.SpaceId).HasColumnName("space_id");
        builder.Property(c => c.ScopeType).HasColumnName("scope_type")
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<ConstraintScopeType>(v, true));
        builder.Property(c => c.ScopeId).HasColumnName("scope_id");
        builder.Property(c => c.Severity).HasColumnName("severity")
            .HasConversion(v => v.ToString().ToLower(), v => Enum.Parse<ConstraintSeverity>(v, true));
        builder.Property(c => c.RuleType).HasColumnName("rule_type").IsRequired();
        builder.Property(c => c.RulePayloadJson).HasColumnName("rule_payload_json")
            .HasColumnType("jsonb");
        builder.Property(c => c.IsActive).HasColumnName("is_active");
        builder.Property(c => c.EffectiveFrom).HasColumnName("effective_from");
        builder.Property(c => c.EffectiveUntil).HasColumnName("effective_until");
        builder.Property(c => c.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(c => c.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
    }
}
