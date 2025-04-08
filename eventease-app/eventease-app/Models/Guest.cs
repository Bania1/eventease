using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace eventease_app.Models
{
    public class Guest
    {
        public int Id { get; set; } // PK

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User? User { get; set; }

        [ForeignKey("Event")]
        public int EventId { get; set; }
        public Event? Event { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
