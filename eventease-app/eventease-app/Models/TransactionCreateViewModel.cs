using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace eventease_app.Models
{
    public class TransactionCreateViewModel : IValidatableObject
    {
        [Required]
        public int EventId { get; set; }

        [Display(Name = "Ticket Price")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }

        // Optional credit card fields
        [Display(Name = "Card Number")]
        [CreditCard]
        public string? CardNumber { get; set; }

        [Display(Name = "Expiration (MM/YY)")]
        public string? Expiration { get; set; }

        [Display(Name = "CVV")]
        public string? Cvv { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PaymentMethod == "CreditCard")
            {
                // Card number required
                if (string.IsNullOrWhiteSpace(CardNumber))
                    yield return new ValidationResult(
                        "Card number is required.",
                        new[] { nameof(CardNumber) });

                // Expiration required and format MM/YY
                if (string.IsNullOrWhiteSpace(Expiration))
                {
                    yield return new ValidationResult(
                        "Expiration date is required.",
                        new[] { nameof(Expiration) });
                }
                else if (!Regex.IsMatch(Expiration, @"^(0[1-9]|1[0-2])\/\d{2}$"))
                {
                    yield return new ValidationResult(
                        "Expiration must be in MM/YY format.",
                        new[] { nameof(Expiration) });
                }

                // CVV required and 3–4 digits
                if (string.IsNullOrWhiteSpace(Cvv))
                {
                    yield return new ValidationResult(
                        "CVV is required.",
                        new[] { nameof(Cvv) });
                }
                else if (!Regex.IsMatch(Cvv, @"^\d{3,4}$"))
                {
                    yield return new ValidationResult(
                        "CVV must be 3 or 4 digits.",
                        new[] { nameof(Cvv) });
                }
            }
        }
    }
}
