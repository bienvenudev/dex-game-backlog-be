using DexGameBacklog.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DexGameBacklog.Api.Data;

// public class AppDbContext : DbContext
// {
//     public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
//     {
//     }
//     
//     public DbSet<User> Users { get; set; }
//     public DbSet<Game> Games { get; set; }
//     public DbSet<Objective> Objectives { get; set; }
//     public DbSet<GameStatusHistory> GameStatusHistories { get; set; }
// }

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Objective> Objectives => Set<Objective>();
    public DbSet<GameStatusHistory> GameStatusHistories => Set<GameStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

