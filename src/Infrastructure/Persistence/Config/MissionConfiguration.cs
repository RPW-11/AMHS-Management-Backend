using Domain.Entities;
using Domain.ValueObjects.Mission;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Config;

public class MissionConfiguration : IEntityTypeConfiguration<Mission>
{
    public void Configure(EntityTypeBuilder<Mission> builder)
    {
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
    }
}
