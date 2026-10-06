using DexGameBacklog.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DexGameBacklog.Api.Data;

public class GameStatusHistoryConfiguration : IEntityTypeConfiguration<GameStatusHistory>
{
    public void Configure(EntityTypeBuilder<GameStatusHistory> builder)
    {
        builder.HasKey(gameStatus => gameStatus.Id);

        builder.Property(gameStatus => gameStatus.GameId)
            .IsRequired();
        
        builder.Property(history => history.Comment)
            .HasMaxLength(500);

        builder.Property(history => history.PreviousStatus)
            .IsRequired();

        builder.Property(history => history.NewStatus)
            .IsRequired();

        builder.Property(history => history.ChangedBy)
            .IsRequired();

        builder.Property(history => history.ChangedAt)
            .IsRequired();
    }
}