using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Config;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FirstName)
        .HasMaxLength(DataSchemaConstants.NAME_LENGTH)
        .IsRequired();

        builder.Property(e => e.LastName)
        .HasMaxLength(DataSchemaConstants.NAME_LENGTH)
        .IsRequired();

        builder.Property(e => e.HashedPassword)
        .HasColumnName("Password")
        .IsRequired();

        builder.Property(e => e.Position)
        .HasColumnName("Position")
        .HasConversion(
            e => e.ToStringValue(),
            e => e.ToEmployeePosition()
        )
        .HasMaxLength(DataSchemaConstants.POSITION_LENGTH)
        .IsRequired();

        builder.Property(e => e.Status)
        .HasMaxLength(DataSchemaConstants.STATUS_LENGTH)
        .IsRequired();;
    }
}
