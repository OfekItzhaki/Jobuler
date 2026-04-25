using Jobuler.Domain.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobuler.Infrastructure.Persistence.Configurations;

public class GroupAlertConfiguration : IEntityTypeConfiguration<GroupAlert>
{
    public void Configure(EntityTypeBuilder<GroupAlert> builder)
    {
        builder.ToTable("group_alerts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.SpaceId).HasColumnName("space_id");
        builder.Property(a => a.GroupId).HasColumnName("group_id");
        builder.Property(a => a.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(a => a.Body).HasColumnName("body").IsRequired();
        builder.Property(a => a.Severity).HasColumnName("severity")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.CreatedByPersonId).HasColumnName("created_by_person_id");
    }
}
