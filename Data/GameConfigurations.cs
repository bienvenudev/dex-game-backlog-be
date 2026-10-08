using DexGameBacklog.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DexGameBacklog.Api.Data;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(game => game.Id);

        builder.Property(game => game.UserId)
            .IsRequired();

        builder.Property(game => game.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(game => game.Notes)
            .HasMaxLength(4000);

        builder.Property(game => game.CoverUrl)
            .HasMaxLength(2048);

        builder.Property(game => game.BackgroundUrl)
            .HasMaxLength(2048);

        builder.HasIndex(game => new { game.UserId, game.Title, game.Platform })
            .IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(game => game.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<Objective>()
            .WithOne()
            .HasForeignKey(objective => objective.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<GameStatusHistory>()
            .WithOne()
            .HasForeignKey(history => history.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}