using BankLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankLite.Infrastructure.Data;

public class BankLiteDbContext : DbContext
{
    public BankLiteDbContext(DbContextOptions<BankLiteDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>()
            .Property(a => a.Balance)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Account>()
            .Property<uint>("xmin")
            .IsRowVersion();

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.CreatedAt);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => new { t.AccountId, t.CreatedAt });

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => new { t.AccountId, t.Type, t.CreatedAt });

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasMany(u => u.Accounts)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.PasswordResetTokens)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Account>()
            .HasMany(a => a.Transactions)
            .WithOne(t => t.Account)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Account>()
            .HasIndex(a => a.AccountNumber)
            .IsUnique();

        modelBuilder.Entity<Account>()
            .HasIndex(a => new { a.UserId, a.Type })
            .IsUnique();

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.UserId);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.PerformedAt);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.UserId);

        modelBuilder.Entity<PasswordResetToken>()
            .HasIndex(p => p.Token)
            .IsUnique();

        modelBuilder.Entity<PasswordResetToken>()
            .HasIndex(p => p.UserId);
    }
}
