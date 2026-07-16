using MailSender.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MailSender.Infrastructure.Persistence;

public class MailSenderDbContext : DbContext
{
    public MailSenderDbContext(DbContextOptions<MailSenderDbContext> options): base(options)
    {
        
    }

    public DbSet<ClientApplication> ClientApplications => Set<ClientApplication>();

    public DbSet<MailSendLog> MailSendLogs => Set<MailSendLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientApplication>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.AppId).IsRequired();

            entity.Property(x => x.AppName).IsRequired();

            entity.HasIndex(x => x.AppId).IsUnique();

            entity.HasIndex(x => x.AppName).IsUnique();
        });

        modelBuilder.Entity<MailSendLog>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.AppId).IsRequired();

            entity.Property(x => x.AppName).IsRequired();

            entity.Property(x => x.To).IsRequired();

            entity.Property(x => x.Subject).IsRequired();

            entity.Property(x => x.Body).IsRequired();

            entity.Property(x => x.Status).IsRequired();
        });
    }
}