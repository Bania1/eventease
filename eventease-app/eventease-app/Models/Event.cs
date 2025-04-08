using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace eventease_app.Models
{
    public class Event
    {
        public int Id { get; set; } // PK

        [ForeignKey("Organizer")]
        public int OrganizerId { get; set; }
        public User? Organizer { get; set; }  // The user who organizes this event

        public string? Theme { get; set; }
        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        // Specify decimal precision via Column attribute:
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public bool IsPublished { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Guest>? Guests { get; set; }
        public ICollection<Ticket>? Tickets { get; set; }
        public ICollection<Transaction>? Transactions { get; set; }
    }
}
