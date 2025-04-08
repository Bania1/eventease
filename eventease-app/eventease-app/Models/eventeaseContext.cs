using Microsoft.EntityFrameworkCore;

namespace eventease_app.Models
{
    public class EventEaseContext : DbContext
    {
        public EventEaseContext(DbContextOptions<EventEaseContext> options)
            : base(options)
        {
        }

        // Expose each entity/table as a DbSet
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Notification> Notifications { get; set; } = default!;
        public DbSet<Event> Events { get; set; } = default!;
        public DbSet<Guest> Guests { get; set; } = default!;
        public DbSet<Ticket> Tickets { get; set; } = default!;
        public DbSet<Transaction> Transactions { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //
            // 1) Unique composite index on (EventId, UserId) in Guest
            //
            modelBuilder.Entity<Guest>()
                .HasIndex(g => new { g.EventId, g.UserId })
                .IsUnique();

            //
            // 2) Configure SELECTIVE CASCADE or RESTRICT to avoid multiple cascade paths
            //

            // A) Cascade from USER to NOTIFICATIONS
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // B) Cascade from USER to ORGANIZED EVENTS
            //    If a user is deleted, that user's events are also deleted.
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Cascade);

            // C) Cascade from EVENT to GUESTS
            //    Deleting an event removes its guest records automatically.
            modelBuilder.Entity<Guest>()
                .HasOne(g => g.Event)
                .WithMany(e => e.Guests)
                .HasForeignKey(g => g.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // D) Cascade from EVENT to TICKETS
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Event)
                .WithMany(e => e.Tickets)
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // E) Cascade from EVENT to TRANSACTIONS
            modelBuilder.Entity<Transaction>()
                .HasOne(tr => tr.Event)
                .WithMany(e => e.Transactions)
                .HasForeignKey(tr => tr.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            //
            // 3) Relationships that must be RESTRICT to avoid cycles:
            //    If we also cascaded from these child entities back to User,
            //    we'd have multiple paths leading to the same table.
            //

            // Guest -> User
            modelBuilder.Entity<Guest>()
                .HasOne(g => g.User)
                .WithMany(u => u.Guests)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ticket -> User
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tickets)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Transaction -> User
            modelBuilder.Entity<Transaction>()
                .HasOne(tr => tr.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(tr => tr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            //
            // 4) OPTIONAL: decimal precision via Fluent API
            //    (If not using [Column(TypeName="decimal(10,2)")] in the model.)
            //
            // modelBuilder.Entity<Event>()
            //     .Property(e => e.Price)
            //     .HasColumnType("decimal(10,2)");
            //
            // modelBuilder.Entity<Transaction>()
            //     .Property(t => t.Amount)
            //     .HasColumnType("decimal(10,2)");
        }
    }
}
