using DexGameBacklog.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DexGameBacklog.Api.Data;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Username)
            .IsRequired()
            .HasMaxLength(100);

        // Login looks a user up by name, so two rows with one name would break it.
        // Also stops replicas that seed at the same time from creating duplicates.
        builder.HasIndex(user => user.Username)
            .IsUnique();
    }
}