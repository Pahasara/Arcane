using Arcane.Core.Models.Entities;
using Arcane.Core.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Arcane.Core.Data;

public sealed class ArcaneDbContext(DbContextOptions<ArcaneDbContext> options) : DbContext(options)
{
    public DbSet<VaultProfile> VaultProfiles => Set<VaultProfile>();
    public DbSet<Entry>        Entries       => Set<Entry>();
    public DbSet<Tag>          Tags          => Set<Tag>();
    public DbSet<EntryTag>     EntryTags     => Set<EntryTag>();
    public DbSet<Attachment>   Attachments   => Set<Attachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VaultProfile>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Salt).IsRequired();
            e.Property(v => v.VerificationCiphertext).IsRequired();
            e.Property(v => v.VerificationNonce).IsRequired();
        });

        modelBuilder.Entity<Entry>(e =>
        {
            e.HasKey(en => en.Id);
            e.Property(en => en.TitleEncrypted).IsRequired();
            e.Property(en => en.TitleNonce).IsRequired();
            e.Property(en => en.ContentEncrypted).IsRequired();
            e.Property(en => en.ContentNonce).IsRequired();

            // Store MoodLevel enum as its integer value (1–5)
            e.Property(en => en.Mood)
             .HasConversion(
                 m => m.HasValue ? (int?)m.Value : null,
                 v => v.HasValue ? (MoodLevel?)v.Value : null);
        });

        modelBuilder.Entity<Tag>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(100);
            e.Property(t => t.ColorHex).HasMaxLength(7);
        });

        modelBuilder.Entity<EntryTag>(e =>
        {
            e.HasKey(et => new { et.EntryId, et.TagId });

            e.HasOne(et => et.Entry)
             .WithMany(en => en.EntryTags)
             .HasForeignKey(et => et.EntryId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(et => et.Tag)
             .WithMany(t => t.EntryTags)
             .HasForeignKey(et => et.TagId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Attachment>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.FileNameEncrypted).IsRequired();
            e.Property(a => a.FileNameNonce).IsRequired();
            e.Property(a => a.MimeType).IsRequired().HasMaxLength(127);

            e.HasOne(a => a.Entry)
             .WithMany(en => en.Attachments)
             .HasForeignKey(a => a.EntryId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
