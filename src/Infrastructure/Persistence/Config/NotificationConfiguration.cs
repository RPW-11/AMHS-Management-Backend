using Domain.Employees;
using Domain.Employees.ValueObjects;
using Domain.Notification;
using Domain.Notification.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Config;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
        .HasConversion(
            n => n.ToString(),
            n => NotificationId.FromString(n).Value
        );

        builder.Property(n => n.RecipientId)
        .HasConversion(
            n => n.ToString(),
            n => EmployeeId.FromString(n).Value
        )
        .HasColumnName("RecipientId")
        .IsRequired();

        builder.HasOne<Employee>()                    
                .WithMany()
                .HasForeignKey(n => n.RecipientId)
                .OnDelete(DeleteBehavior.NoAction)   
                .HasConstraintName("FK_Notification_Recipient_Employee");

        builder.Property(n => n.ActorId)
        .HasConversion(
            id => id.ToString(),
            value => string.IsNullOrEmpty(value) 
                ? null 
                : EmployeeId.FromString(value).Value
        )
        .HasColumnName("ActorId");

        builder.HasOne<Employee>()                    
                .WithMany()
                .HasForeignKey(n => n.ActorId)
                .OnDelete(DeleteBehavior.NoAction)   
                .HasConstraintName("FK_Notification_Actor_Employee");

        builder.Property(e => e.ActorName).HasMaxLength(120).IsRequired();
        builder.Property(e => e.ActorAvatarUrl).HasMaxLength(500);

        builder.ComplexProperty(n => n.NotificationTarget, target =>
        {
            target.Property(t => t.Type)
                .HasColumnName("TargetType")
                .HasMaxLength(50)
                .IsRequired();

            target.Property(t => t.Id)
                .HasColumnName("TargetId")
                .IsRequired();
        });

        builder.Property(n => n.NotificationType)
        .HasConversion(
            n => n.ToString(),
            n => NotificationType.FromString(n).Value
        )
        .IsRequired();

        builder.Property(e => e.ReadAt);

        // Critical indexes
        builder.HasIndex(e => new { e.RecipientId, e.ReadAt, e.CreatedAt })
               .HasDatabaseName("Notifications_Recipient_Unread_Recent");

        builder.HasIndex(e => e.RecipientId);

    }
}