using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Persistance.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public const string EmailIndexName = "IX_Users_Email";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.Name).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(30);
        builder.Property(u => u.CreatedAt);
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName(EmailIndexName);
    }
}
