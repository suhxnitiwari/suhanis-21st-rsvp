using Microsoft.EntityFrameworkCore;
using PartyRsvp.Models;

namespace PartyRsvp.Data;

public class PartyDbContext(DbContextOptions<PartyDbContext> options) : DbContext(options)
{
    public DbSet<Rsvp> Rsvps => Set<Rsvp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rsvp>(rsvp =>
        {
            rsvp.HasIndex(r => r.Email).IsUnique();
            rsvp.Property(r => r.Name).HasMaxLength(80);
            rsvp.Property(r => r.Email).HasMaxLength(254);
            rsvp.Property(r => r.Phone).HasMaxLength(30);
            rsvp.Property(r => r.DietaryNotes).HasMaxLength(200);
            rsvp.Property(r => r.Message).HasMaxLength(280);
        });
    }
}
