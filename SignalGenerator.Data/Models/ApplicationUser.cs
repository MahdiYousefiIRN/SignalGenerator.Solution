using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SignalGenerator.Data.Models
{
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// The user's first name.
        /// </summary>
        [Required]
        [StringLength(100)]
        public required string FirstName { get; set; }

        /// <summary>
        /// The user's last name.
        /// </summary>
        [Required]
        [StringLength(100)]
        public required string LastName { get; set; }

        /// <summary>
        /// The user's organization.
        /// </summary>
        [StringLength(500)]
        public string? Organization { get; set; }

        /// <summary>
        /// The timestamp when the user was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The timestamp of the user's last login.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// A boolean indicating whether the user is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// A collection of signal groups associated with the user.
        /// </summary>
        public virtual ICollection<SignalGroup> SignalGroups { get; set; } = new List<SignalGroup>();
    }
} 