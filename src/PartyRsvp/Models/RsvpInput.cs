using System.ComponentModel.DataAnnotations;

namespace PartyRsvp.Models;

/// <summary>What the RSVP form posts. Kept separate from <see cref="Rsvp"/> so users can't over-post fields like Id.</summary>
public class RsvpInput
{
    public const int MaxPartySize = 4;

    [Required(ErrorMessage = "Please enter your name")]
    [StringLength(80, ErrorMessage = "Please keep your name under 80 characters")]
    [Display(Name = "Your name")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Please enter your email address")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address")]
    [StringLength(254)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Please enter your phone number")]
    [RegularExpression(@"^\+?[0-9()\-.\s]{7,20}$", ErrorMessage = "That doesn't look like a phone number")]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Please let us know whether you'll attend")]
    [Display(Name = "Will you attend?")]
    public bool? WillAttend { get; set; }

    [Range(1, MaxPartySize, ErrorMessage = "Party size must be between 1 and 4")]
    [Display(Name = "Party size")]
    public int PartySize { get; set; } = 1;

    [StringLength(200, ErrorMessage = "Please keep dietary notes under 200 characters")]
    [Display(Name = "Dietary needs")]
    public string? DietaryNotes { get; set; }

    [StringLength(280, ErrorMessage = "Please keep your wish under 280 characters")]
    [Display(Name = "A birthday wish")]
    public string? Message { get; set; }
}
