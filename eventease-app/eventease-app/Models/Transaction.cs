using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace eventease_app.Models
{
    public class Transaction
    {
        public int Id { get; set; } // PK

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User? User { get; set; }

        [ForeignKey("Event")]
        public int EventId { get; set; }
        public Event? Event { get; set; }

        // Specify decimal precision via Column attribute:
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public string? PaymentMethod { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
