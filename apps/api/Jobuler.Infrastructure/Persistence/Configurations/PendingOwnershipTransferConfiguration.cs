using Jobuler.Domain.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobuler.Infrastructure.Persistence.Configurations;

public class PendingOwnershipTransferConfiguration : IEntityTypeConfiguration<PendingOwnershipTransfer>
{
    public void Configure(EntityTypeBuilder<PendingOwnershipTransfer> builder)
    {
        builder.ToTable("pending_ownership_transfers");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.SpaceId).HasColumnName("space_id").IsRequired();
        builder.Property(t => t.GroupId).HasColumnName("group_id").IsRequired();
        builder.Property(t => t.CurrentOwnerPersonId).HasColumnName("current_owner_person_id").IsRequired();
        builder.Property(t => t.ProposedOwnerPersonId).HasColumnName("proposed_owner_person_id").IsRequired();
        builder.Property(t => t.ConfirmationToken).HasColumnName("confirmation_token").HasMaxLength(64).IsRequired();
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(t => t.ConfirmationToken).IsUnique();
        builder.HasIndex(t => t.GroupId).IsUnique();
    }
}
