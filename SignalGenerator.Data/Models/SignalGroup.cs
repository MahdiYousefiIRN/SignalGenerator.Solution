using System.ComponentModel.DataAnnotations;
using SignalGenerator.Data.Models;

namespace SignalGenerator.Data.Models
{
    public class SignalGroup
    {
        /// <summary>
        /// Unique identifier for the signal group.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the signal group.
        /// </summary>
        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        /// <summary>
        /// A description of the signal group.
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// The timestamp when the signal group was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The ID of the user who created the signal group.
        /// </summary>
        public required string CreatedById { get; set; }

        /// <summary>
        /// A reference to the user who created the signal group.
        /// </summary>
        public required virtual ApplicationUser CreatedBy { get; set; }

        /// <summary>
        /// A collection of users who are members of the signal group.
        /// </summary>
        public virtual ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
    }
} 