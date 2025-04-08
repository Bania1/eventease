using eventease_app.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace eventease_app.Models
{
    public class Notification
    {
        public int Id { get; set; }  // PK

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User? User { get; set; }  // Navigation property

        public string Message { get; set; } = default!;
        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

