using Domain.Employees;
using Domain.Missions;
using Domain.Missions.Entities;
using Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
    : base(options)
    {

    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<MissionBase> Missions { get; set; }
    public DbSet<AssignedEmployee> AssignedEmployees { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
