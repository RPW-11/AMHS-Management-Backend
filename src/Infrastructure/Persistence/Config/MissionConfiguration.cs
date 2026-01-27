using Domain.Missions;
using Domain.Missions.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Config;

public class MissionConfiguration : IEntityTypeConfiguration<MissionBase>
{
    public void Configure(EntityTypeBuilder<MissionBase> builder)
    {
        builder.Property(m => m.Id)
        .HasConversion(
            m => m.ToString(),
            m => MissionId.FromString(m).Value
        );

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
        .HasMaxLength(DataSchemaConstants.Mission.NAME_LENGTH)
        .IsRequired();

        builder.Property(m => m.Description)
        .HasMaxLength(DataSchemaConstants.Mission.DESCRIPTION_LENGTH)
        .HasDefaultValue("");

        builder.Property(m => m.Category)
        .HasConversion(
            m => m.ToString(),
            m => MissionCategory.FromString(m).Value
        )
        .HasMaxLength(DataSchemaConstants.Mission.CATEGORY_LENGTH)
        .IsRequired();

        builder.Property(m => m.Status)
        .HasConversion(
            m => m.ToString(),
            m => MissionStatus.FromString(m).Value
        )
        .HasMaxLength(DataSchemaConstants.Mission.STATUS_LENGTH)
        .IsRequired();

        builder.HasMany(m => m.AssignedEmployees)
        .WithOne()
        .HasForeignKey(ae => ae.MissionId);
    }
}
