using DexGameBacklog.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DexGameBacklog.Api.Data;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.Property(game => game.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany<GameStatusHistory>()
            .WithOne()
            .HasForeignKey(history => history.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}