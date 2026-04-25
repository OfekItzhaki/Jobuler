using Jobuler.Domain.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobuler.Infrastructure.Persistence.Configurations;

public class GroupMessageConfiguration : IEntityTypeConfiguration<GroupMessage>
{
    public void Configure(EntityTypeBuilder<GroupMessage> builder)
    {
        builder.ToTable("group_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.SpaceId).HasColumnName("space_id").IsRequired();
        builder.Property(m => m.GroupId).HasColumnName("group_id").IsRequired();
        builder.Property(m => m.AuthorUserId).HasColumnName("author_user_id").IsRequired();
        builder.Property(m => m.Content).HasColumnName("content").IsRequired();
        builder.Property(m => m.IsPinned).HasColumnName("is_pinned");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");
    }
}
