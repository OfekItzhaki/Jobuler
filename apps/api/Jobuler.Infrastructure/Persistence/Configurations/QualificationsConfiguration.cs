using Jobuler.Domain.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jobuler.Infrastructure.Persistence.Configurations;

public class GroupQualificationConfiguration : IEntityTypeConfiguration<GroupQualification>
{
    public void Configure(EntityTypeBuilder<GroupQualification> builder)
    {
        builder.ToTable("group_qualifications");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).HasColumnName("id");
        builder.Property(q => q.SpaceId).HasColumnName("space_id");
        builder.Property(q => q.GroupId).HasColumnName("group_id");
        builder.Property(q => q.Name).HasColumnName("name").IsRequired();
        builder.Property(q => q.Description).HasColumnName("description");
        builder.Property(q => q.IsActive).HasColumnName("is_active");
        builder.Property(q => q.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(q => q.CreatedAt).HasColumnName("created_at");
        builder.Property(q => q.UpdatedAt).HasColumnName("updated_at");
    }
}

public class MemberQualificationConfiguration : IEntityTypeConfiguration<MemberQualification>
{
    public void Configure(EntityTypeBuilder<MemberQualification> builder)
    {
        builder.ToTable("member_qualifications");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).HasColumnName("id");
        builder.Property(q => q.SpaceId).HasColumnName("space_id");
        builder.Property(q => q.GroupId).HasColumnName("group_id");
        builder.Property(q => q.PersonId).HasColumnName("person_id");
        builder.Property(q => q.QualificationId).HasColumnName("qualification_id");
        builder.Property(q => q.AssignedAt).HasColumnName("assigned_at");
        builder.Property(q => q.AssignedByUserId).HasColumnName("assigned_by_user_id");
        builder.Ignore(q => q.CreatedAt);
    }
}
