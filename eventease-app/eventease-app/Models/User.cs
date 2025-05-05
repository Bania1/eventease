using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace eventease_app.Models
{
    public class User
    {
        public int Id { get; set; }  // PK automatically recognized by EF

        // Basic Info
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;

        // e.g. "admin", "organizer", or "user"
        public string Role { get; set; } = "user";

        public bool Approved { get; set; } = false;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<Notification>? Notifications { get; set; }
        public ICollection<Event>? OrganizedEvents { get; set; } // If a user organizes events
        public ICollection<Guest>? Guests { get; set; }          // If a user is a guest for events
        public ICollection<Ticket>? Tickets { get; set; }        // If a user has tickets
        public ICollection<Transaction>? Transactions { get; set; }

        [NotMapped]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }
    }
}
