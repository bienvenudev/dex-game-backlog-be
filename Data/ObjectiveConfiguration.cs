using DexGameBacklog.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DexGameBacklog.Api.Data;

public class ObjectiveConfiguration : IEntityTypeConfiguration<Objective>
{
    public void Configure(EntityTypeBuilder<Objective> builder)
    {
        builder.HasKey(objective => objective.Id);

        builder.Property(objective => objective.GameId)
            .IsRequired();
        
        builder.Property(objective => objective.Label)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(objective => objective.Position)
            .IsRequired();

        builder.HasIndex(objective => new { objective.GameId, objective.Label })
            .IsUnique();
    }
}