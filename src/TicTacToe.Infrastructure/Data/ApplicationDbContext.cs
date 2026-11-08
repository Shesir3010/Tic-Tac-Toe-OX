using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Domain.Entities;
using TicTacToe.Infrastructure.Identity;

namespace TicTacToe.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Match> Matches => Set<Match>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Match>()
            .HasIndex(m => new { m.UserId, m.PlayedAtUtc })
            .IsDescending(false, true);

        builder.Entity<Match>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Match>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.OpponentUserId)
            .OnDelete(DeleteBehavior.NoAction); // SQL forbids Cascade+SetNull dual FKs to one principal (1785); DeleteAccount nulls OpponentUserId explicitly first
    }
}
