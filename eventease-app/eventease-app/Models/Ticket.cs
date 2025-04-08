using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eventease_app.Models
{
    public class Ticket
    {
        public int Id { get; set; } // PK

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User? User { get; set; }

        [ForeignKey("Event")]
        public int EventId { get; set; }
        public Event? Event { get; set; }

        // e.g. a unique code scanned at the event
        [Required]
        public string QrCode { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

