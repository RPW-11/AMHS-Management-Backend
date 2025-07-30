using Domain.Entities;
using Domain.ValueObjects.Employee;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Config;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FirstName)
        .HasMaxLength(DataSchemaConstants.Employee.NAME_LENGTH)
        .IsRequired();

        builder.Property(e => e.LastName)
        .HasMaxLength(DataSchemaConstants.Employee.NAME_LENGTH)
        .IsRequired();

        builder.Property(e => e.HashedPassword)
        .HasColumnName("Password")
        .IsRequired();

        builder.Property(e => e.Position)
        .HasColumnName("Position")
        .HasConversion(
            e => e.ToString(),
            e => EmployeePosition.FromString(e).Value
        )
        .HasMaxLength(DataSchemaConstants.Employee.POSITION_LENGTH)
        .IsRequired();

        builder.Property(e => e.Status)
        .HasConversion(
            s => s.ToString(),
            s => EmployeeStatus.FromString(s).Value
        )
        .HasMaxLength(DataSchemaConstants.Employee.STATUS_LENGTH)
        .IsRequired();

        builder.Property(e => e.DateOfBirth)
        .HasColumnType("timestamp without time zone");
    }
}
