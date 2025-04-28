using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace eventease_app.Models
{
    // Models/Event.cs
    public class Event
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Organizer))]
        public int OrganizerId { get; set; }
        public User? Organizer { get; set; }
        public string? Location { get; set; }
        public string? Theme { get; set; }   // title
        public string? Description { get; set; }   // short blurb
        public string? LongDescription { get; set; }   // detail page text
        public string? Category { get; set; }   // “Music”, “Conference”, …

        // image file names you drop in wwwroot/img
        public string? ThumbnailFileName { get; set; }  // 300 px card image
        public string? HeroFileName { get; set; }  // big header image

        public DateTime StartDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public bool IsPublished { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // navigation
        public ICollection<Guest>? Guests { get; set; }
        public ICollection<Ticket>? Tickets { get; set; }
        public ICollection<Transaction>? Transactions { get; set; }
    }

}
