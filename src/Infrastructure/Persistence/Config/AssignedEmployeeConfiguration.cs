using Domain.Employees;
using Domain.Employees.ValueObjects;
using Domain.Missions;
using Domain.Missions.Entities;
using Domain.Missions.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Config;

public class AssignedEmployeeConfiguration : IEntityTypeConfiguration<AssignedEmployee>
{
    public void Configure(EntityTypeBuilder<AssignedEmployee> builder)
    {
        builder.Ignore(ae => ae.Id);
        
        builder.Property(ae => ae.MissionId)
        .HasConversion(
            missionId => missionId.ToString(),
            missionIdStr => MissionId.FromString(missionIdStr).Value
        ).IsRequired();

        builder.Property(ae => ae.EmployeeId)
        .HasConversion(
            employeeId => employeeId.ToString(),
            employeeIdStr => EmployeeId.FromString(employeeIdStr).Value
        ).IsRequired();

        builder.HasKey(ae => new { ae.MissionId, ae.EmployeeId });

        builder.Property(ae => ae.MissionRole)
        .HasConversion(
            missionRole => missionRole.ToString(),
            missionRoleStr => MissionRole.FromString(missionRoleStr).Value
        ).IsRequired();

        builder.HasOne<MissionBase>()
        .WithMany(m => m.AssignedEmployees)
        .HasForeignKey(ae => ae.MissionId);

        builder.HasOne<Employee>()
        .WithMany()
        .HasForeignKey(ae => ae.EmployeeId);
    }
}
